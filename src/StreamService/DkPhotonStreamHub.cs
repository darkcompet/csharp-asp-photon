namespace Tool.Compet.Photon {
	using MessagePack;

	public abstract class DkPhotonStreamHub : PhotonHub {
		public DkPhotonStreamHub(int id, PhotonStreamConnector connector) : base(id, connector) {
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
