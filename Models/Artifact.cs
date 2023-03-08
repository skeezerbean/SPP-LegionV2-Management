
namespace SPP_LegionV2_Management
{
	public class Artifact
	{
		public int CharacterGUID { get; set; } //char_guid for item_instance_artifact
		public int ItemGUID { get; set; } // itemGUID for item_instance_artifact
		public long Power { get; set; } // xp for item_instance_artifact

		public Artifact()
		{
		}
	}
}
