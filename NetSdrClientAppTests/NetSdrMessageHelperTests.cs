using NetSdrClientApp.Messages;
using NUnit.Framework;
using System;
using System.Linq;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrMessageHelperTests
    {
        [Test]
        public void GetControlItemMessageTest()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            // Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2).ToArray();
            var codeBytes = msg.Skip(2).Take(2).ToArray();
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes);
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes);

            // Assert
            // Використовуємо Assert.Multiple для групування перевірок (вимога Лаби 2/3)
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes, Has.Length.EqualTo(2)); // Використовуємо Has.Length
                Assert.That(msg.Length, Is.EqualTo(actualLength));
                Assert.That(actualType, Is.EqualTo(type));
                Assert.That(actualCode, Is.EqualTo((short)code));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            // Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2).ToArray();
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes);
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes, Has.Length.EqualTo(2));
                Assert.That(msg.Length, Is.EqualTo(actualLength));
                Assert.That(actualType, Is.EqualTo(type));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        // --- НОВІ ТЕСТИ ДЛЯ ЛАБИ 3 ---

        [Test]
        public void GetControlItemMessage_WithEmptyParameters_ReturnsMinimumLength()
        {
            // Перевірка мінімального повідомлення (заголовок 2 байти + код 2 байти = 4)
            var type = NetSdrMessageHelper.MsgTypes.Request;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, Array.Empty<byte>());

            Assert.That(msg, Has.Length.EqualTo(4));
        }

        [Test]
        public void GetDataItemMessage_WithNullParameters_ThrowsException()
        {
            // Перевірка стійкості до помилок (якщо передали null)
            var type = NetSdrMessageHelper.MsgTypes.DataItem1;

            Assert.Throws<ArgumentNullException>(() => 
                NetSdrMessageHelper.GetDataItemMessage(type, null!));
        }

        [Test]
        public void GetControlItemMessage_LargePayload_CalculatesLengthCorrectly()
        {
            // Перевірка на великий обсяг даних (близько до максимуму 13 біт довжини)
            var type = NetSdrMessageHelper.MsgTypes.Set;
            int largeSize = 8000; 
            
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, NetSdrMessageHelper.ControlItemCodes.ReceiverState, new byte[largeSize]);

            var num = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var actualLength = num & 0x1FFF; // маска для 13 біт довжини

            Assert.That(msg.Length, Is.EqualTo(actualLength));
        }

        [Test]
        public void MsgTypes_Enum_HasExpectedValues()
        {
            // Перевірка коректності значень Enum (важливо для бітових зсувів)
            Assert.Multiple(() =>
            {
                Assert.That((int)NetSdrMessageHelper.MsgTypes.Request, Is.EqualTo(0));
                Assert.That((int)NetSdrMessageHelper.MsgTypes.Ack, Is.EqualTo(4));
            });
        }
    }
}            
