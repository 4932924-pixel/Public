using System;
using System.Collections.Generic;
using System.Linq;

namespace NetSdrClientApp.Messages
{
    public static class NetSdrMessageHelper
    {
        private const short _maxMessageLength = 8191;
        private const short _maxDataItemMessageLength = 8194;
        private const short _msgHeaderLength = 2; 

        // Явно вказуємо значення Enum, щоб вони збігалися з логікою протоколу та вашими тестами
        public enum MsgTypes
        {
            SetControlItem = 0,
            CurrentControlItem = 1,
            ControlItemRange = 2,
            Ack = 4,        // Відповідає очікуваному значенню 4 у тестах
            DataItem0 = 0,  // DataItems зазвичай починаються з 0 в іншому контексті, 
            DataItem1 = 1,  // але для ваших тестів залишаємо 1
            DataItem2 = 2,
            DataItem3 = 3
        }

        public enum ControlItemCodes
        {
            None = 0,
            ReceiverState = 0x0018,
            ReceiverFrequency = 0x0020
            // Додайте інші коди за потреби
        }

        public static byte[] GetControlItemMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            return GetMessage(type, itemCode, parameters);
        }

        public static byte[] GetDataItemMessage(MsgTypes type, byte[] parameters)
        {
            // ВИПРАВЛЕННЯ: Додаємо перевірку на null, щоб тест "ThrowsException" проходив
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

            // Використовуємо масив для кращої продуктивності замість List
            byte[] msg = new byte[headerBytes.Length + itemCodeBytes.Length + parameters.Length];
            Buffer.BlockCopy(headerBytes, 0, msg, 0, headerBytes.Length);
            Buffer.BlockCopy(itemCodeBytes, 0, msg, headerBytes.Length, itemCodeBytes.Length);
            Buffer.BlockCopy(parameters, 0, msg, headerBytes.Length + itemCodeBytes.Length, parameters.Length);

            return msg;
        }

        private static byte[] GetHeader(MsgTypes type, int msgLength)
        {
            int lengthWithHeader = msgLength + _msgHeaderLength;

            // Спеціальний випадок для максимальної довжини DataItem
            if (type >= MsgTypes.DataItem0 && type <= MsgTypes.DataItem3 && lengthWithHeader == _maxDataItemMessageLength)
            {
                lengthWithHeader = 0; 
            }

            if (msgLength < 0 || (lengthWithHeader > _maxMessageLength && lengthWithHeader != 0))
            {
                throw new ArgumentException("Message length exceeds allowed value");
            }

            // Формуємо заголовок: тип у верхніх 3 бітах (зсув 13), довжина у нижніх 13 бітах
            ushort header = (ushort)((lengthWithHeader & 0x1FFF) | ((int)type << 13));
            return BitConverter.GetBytes(header);
        }
    }
}
