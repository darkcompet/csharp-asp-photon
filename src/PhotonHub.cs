namespace Tool.Compet.Photon {
	public abstract class PhotonHub {
		public readonly int id;
		public PhotonStreamConnector connector;

		public PhotonHub(int id, PhotonStreamConnector connector) {
			this.id = id;
			this.connector = connector;
		}
	}
}
