using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientApp.Messages
{
    public static class NetSdrMessageHelper
    {
        private const short _maxMessageLength = 8191;
        private const short _maxDataItemMessageLength = 8194;
        private const short _msgHeaderLength = 2; 
        private const short _msgControlItemLength = 2; 
        private const short _msgSequenceNumberLength = 2; 

        public enum MsgTypes
        {
            SetControlItem = 0,
            CurrentControlItem = 1,
            ControlItemRange = 2,
            Ack = 3,       // Відповідає очікуваному значенню 3 у тестах
            DataItem0 = 4,
            DataItem1 = 5, // Відповідає очікуваному значенню 5 у тестах
            DataItem2 = 6,
            DataItem3 = 7
        }

        public enum ControlItemCodes
        {
            None = 0,
            IQOutputDataSampleRate = 0x00B8,
            RFFilter = 0x0044,
            ADModes = 0x008A,
            ReceiverState = 0x0018,
            ReceiverFrequency = 0x0020
        }

        public static byte[] GetControlItemMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            // Виправляє NullReferenceException у тестах
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            return GetMessage(type, itemCode, parameters);
        }

        public static byte[] GetDataItemMessage(MsgTypes type, byte[] parameters)
        {
            // Виправляє NullReferenceException у тестах
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            return GetMessage(type, ControlItemCodes.None, parameters);
        }

        private static byte[] GetMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            var itemCodeBytes = Array.Empty<byte>();
            if (itemCode != ControlItemCodes.None)
            {
                itemCodeBytes = BitConverter.GetBytes((ushort)itemCode);
            }

            var headerBytes = GetHeader(type, itemCodeBytes.Length + parameters.Length);

            List<byte> msg = new List<byte>();
            msg.AddRange(headerBytes);
            msg.AddRange(itemCodeBytes);
            msg.AddRange(parameters);

            return msg.ToArray();
        }

        public static bool TranslateMessage(byte[] msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort sequenceNumber, out byte[] body)
        {
            if (msg == null || msg.Length < _msgHeaderLength)
            {
                type = MsgTypes.SetControlItem;
                itemCode = ControlItemCodes.None;
                sequenceNumber = 0;
                body = Array.Empty<byte>();
                return false;
            }

            itemCode = ControlItemCodes.None;
            sequenceNumber = 0;
            bool success = true;
            
            IEnumerable<byte> msgEnumerable = msg;

            TranslateHeader(msgEnumerable.Take(_msgHeaderLength).ToArray(), out type, out int msgLength);
            msgEnumerable = msgEnumerable.Skip(_msgHeaderLength);
            msgLength -= _msgHeaderLength;

            if (type < MsgTypes.DataItem0) 
            {
                var codeArr = msgEnumerable.Take(_msgControlItemLength).ToArray();
                if (codeArr.Length < _msgControlItemLength)
                {
                    body = Array.Empty<byte>();
                    return false;
                }

                var value = BitConverter.ToUInt16(codeArr);
                msgEnumerable = msgEnumerable.Skip(_msgControlItemLength);
                msgLength -= _msgControlItemLength;

                if (Enum.IsDefined(typeof(ControlItemCodes), (int)value))
                {
                    itemCode = (ControlItemCodes)value;
                }
                else
                {
                    success = false;
                }
            }
            else 
            {
                var seqArr = msgEnumerable.Take(_msgSequenceNumberLength).ToArray();
                if (seqArr.Length >= _msgSequenceNumberLength)
                {
                    sequenceNumber = BitConverter.ToUInt16(seqArr);
                    msgEnumerable = msgEnumerable.Skip(_msgSequenceNumberLength);
                    msgLength -= _msgSequenceNumberLength;
                }
            }

            body = msgEnumerable.ToArray();
            success &= body.Length == msgLength;

            return success;
        }

        public static IEnumerable<int> GetSamples(ushort sampleSizeBits, byte[] body)
        {
            int sampleSizeBytes = sampleSizeBits / 8; 
            if (sampleSizeBytes > 4 || sampleSizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSizeBits));
            }

            if (body == null) yield break;

            byte[] prefixBytes = new byte[4 - sampleSizeBytes];

            for (int i = 0; i <= body.Length - sampleSizeBytes; i += sampleSizeBytes)
            {
                byte[] sample = new byte[4];
                Array.Copy(body, i, sample, 0, sampleSizeBytes);
                Array.Copy(prefixBytes, 0, sample, sampleSizeBytes, prefixBytes.Length);
                
                yield return BitConverter.ToInt32(sample, 0);
            }
        }

        private static byte[] GetHeader(MsgTypes type, int msgLength)
        {
            int lengthWithHeader = msgLength + 2;

            if (type >= MsgTypes.DataItem0 && lengthWithHeader == _maxDataItemMessageLength)
            {
                lengthWithHeader = 0;
            }

            if (msgLength < 0 || (lengthWithHeader > _maxMessageLength && lengthWithHeader != 0))
            {
                throw new ArgumentException("Message length exceeds allowed value");
            }

            return BitConverter.GetBytes((ushort)(lengthWithHeader | ((int)type << 13)));
        }

        private static void TranslateHeader(byte[] header, out MsgTypes type, out int msgLength)
        {
            var num = BitConverter.ToUInt16(header, 0);
            type = (MsgTypes)(num >> 13);
            msgLength = num & 0x1FFF; // Використання маски замість віднімання

            if (type >= MsgTypes.DataItem0 && msgLength == 0)
            {
                msgLength = _maxDataItemMessageLength;
            }
        }
    }
}
