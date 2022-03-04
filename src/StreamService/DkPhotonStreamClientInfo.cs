namespace Tool.Compet.Photon {
	public class DkPhotonStreamClientInfo {
		public PhotonStreamConnector connector;

		public DkPhotonStreamClientInfo() {
		}

		public TServiceResponse GetCompanionServiceResponse<TServiceResponse>(TServiceResponse friend) where TServiceResponse : class {
			if (friend is DkPhotonStreamHub terminalHub) {
				return this.connector.hubs[terminalHub.id][terminalHub.terminalId] as TServiceResponse;
			}
			else {
				throw new Exception($"Class {friend.GetType()} must be `DkPhotonStreamHub`.");
			}
		}
	}
}
