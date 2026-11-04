using System;
using System.Collections.Generic;
using CorpPresence.Malice;

namespace CorpPresence;

internal static class SaveStructure
{
	public static void Validate(CorpState state)
	{
		Require(state.Characters != null && state.Companies != null && state.Meds != null, "公司根状态集合缺失");
		if (state.Seline != null)
		{
			Require(state.Seline.SideDone != null, "旧瑟琳支线记录缺失");
		}
		foreach (KeyValuePair<string, CharacterState> character in state.Characters)
		{
			Require(character.Value != null && character.Value.SideDone != null, "人物记录缺失：", character.Key);
		}
		foreach (KeyValuePair<string, CompanyState> company in state.Companies)
		{
			CompanyState value = company.Value;
			Require(value != null, "公司记录缺失：", company.Key);
			Require(value.Brand != null && value.Orders != null && value.LimitedBought != null && value.LabList != null && value.Visitors != null, "公司必要集合缺失：", company.Key);
			foreach (Order order in value.Orders)
			{
				Require(order != null && order.Items != null, "配送明细缺失：", company.Key);
				foreach (string item in order.Items)
				{
					Require(item != null, "配送物品标识为空：", company.Key);
				}
			}
			foreach (Visitor visitor in value.Visitors)
			{
				Require(visitor != null, "预约记录为空：", company.Key);
			}
		}
	}

	public static void Validate(MaliceState state)
	{
		Require(state.Corps != null && state.Envoy != null && state.Rebel != null, "巨企根状态集合缺失");
		foreach (KeyValuePair<string, CorpTerms> item in state.Corps)
		{
			Require(item.Value != null, "巨企合约记录缺失：", item.Key);
		}
		Require(state.Envoy.Coins != null && state.Envoy.Visits != null, "特使必要集合缺失");
		foreach (EnvoyVisit visit in state.Envoy.Visits)
		{
			Require(visit != null, "特使预约记录为空");
		}
	}

	private static void Require(bool valid, string message, string key = null)
	{
		if (!valid)
		{
			throw new InvalidOperationException((key == null) ? message : (message + key));
		}
	}
}
