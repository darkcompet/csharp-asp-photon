namespace Tool.Compet.Photon {
	public class DkPhotonStreamClient {
		/// Internal set at configuration time.
		public PhotonStreamConnector connector;

		public TServiceResponse GetCompanionServiceResponse<TServiceResponse>(TServiceResponse friend) where TServiceResponse : class {
			if (friend is DkPhotonStreamHub terminalHub) {
				return this.connector.hubs[terminalHub.id][terminalHub.terminalId] as TServiceResponse;
			}
			else {
				throw new Exception($"Could not find companion service response for given class {friend.GetType()}.");
			}
		}
	}
}
