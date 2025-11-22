private List<KeyValuePair<string, int>> GetTotalComponents(IMyProjector projector)
{
	var blocks = projector.RemainingBlocksPerType;
	var total = new Dictionary<string, int>();
	foreach (var item in blocks)
	{
		var info = item.ToString().Trim('[', ']').Split(',');
		string blockName = info[0].Replace(" ", "");
		int amount = Convert.ToInt32(info[1]);
		AddComponents(total, GetComponents(blockName), amount);
	}

	bool large = projector.BlockDefinition.SubtypeId == "LargeProjector";
	string armorType = "MyObjectBuilder_CubeBlock/" +
		(large
			? (lightArmor ? "LargeBlockArmorBlock" : "LargeHeavyBlockArmorBlock")
			: (lightArmor ? "SmallBlockArmorBlock" : "SmallHeavyBlockArmorBlock"));
	AddComponents(total, GetComponents(armorType), projector.RemainingArmorBlocks);

	var list = total.ToList();
	list.Sort((x, y) => string.Compare(TranslateDef(x.Key), TranslateDef(y.Key)));
	return list;
}

private string TranslateDef(string d) => componentTranslation[d.Replace("MyObjectBuilder_BlueprintDefinition/", "")];
