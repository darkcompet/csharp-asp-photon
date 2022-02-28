namespace Tool.Compet.Photon {
	public abstract class PhotonConnector {
		/// Holds all hubs for communication between server and the client.
		public readonly Dictionary<int, PhotonHub> hubs = new();
	}
}
