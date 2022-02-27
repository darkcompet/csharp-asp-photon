namespace Tool.Compet.Photon {
	using MessagePack;

	public abstract class DkPhotonHub {
		public readonly int id;
		public PhotonConnector connector;

		public DkPhotonHub(int id, PhotonConnector connector) {
			this.id = id;
			this.connector = connector;
		}

		internal async Task SendAsync(int methodId, object msgPackObj) {
			// dkopt: consider reduce byte length of message.
			await this.connector.SendAsync(MessagePackSerializer.Serialize(new object[] {
				(byte)DkPhotonMessageType.SERVICE, // message type (service, rpc,...)
				(byte)this.id, // hub id
				(short)methodId, // method id
				msgPackObj // parameters
			}));
		}
	}
}
