
namespace SPP_LegionV2_Management
{
	public class Account
	{
		public int ID { get; set; }
		public string Username { get; set; }
		public int BattleNetAccount { get; set; }
		public string BattleNetEmail { get; set; }
		public int BattleCoins { get; set; } // donate columm in battlenet_accounts
		public int GMLevel { get; set; } // gmlevel in account_access

		public Account() { }
	}
}
