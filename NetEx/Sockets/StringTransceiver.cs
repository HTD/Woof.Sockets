using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Woof.NetEx.Sockets {

    class StringTransceiver : ITransceiver<string> {

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        private readonly BinaryTransceiver B = new BinaryTransceiver();

        public KeyValuePair<ReceiveStatus, string> Receive(NetworkStreamEx stream) {
            var received = B.Receive(stream);
            if (received.Key == ReceiveStatus.Fail) return new KeyValuePair<ReceiveStatus, string>(ReceiveStatus.Fail, null);
            return new KeyValuePair<ReceiveStatus, string>(received.Key, Encoding.GetString(received.Value));
        }

        public void Transmit(NetworkStreamEx stream, string packet) => B.Transmit(stream, Encoding.GetBytes(packet));

    }

}