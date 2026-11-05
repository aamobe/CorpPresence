using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CorpPresence;
internal static partial class Engine
{

	private static StoreClient Create(Character c, string kind)
	{
		CharacterState s = StateOf(c);
		bool flag = !kind.StartsWith("ch");
		Visit v = (flag ? c.Daily(s) : c.Chapter(int.Parse(kind.Substring(2))));
		StoreClient client = new StoreClient
		{
			identifier = c.Prefix + kind,
			displayName = c.Name,
			clientFaction = c.Faction,
			spriteName = Portraits.Ensure(c.Portrait(s, kind), c.PortraitRef),
			clientIntent = (v.TalkOnly ? StoreClient.ClientIntent.DIALOGUE : (v.Both ? StoreClient.ClientIntent.SELLNBUY : (v.Buy ? StoreClient.ClientIntent.BUY : StoreClient.ClientIntent.SELL))),
			dismissable = true
		};
		client.AddBasicDialog();
		if (v.Buy || v.Both)
		{
			StoreClient storeClient = client;
			AddAll(storeClient.clientBuyingTagList ?? (storeClient.clientBuyingTagList = new List<string>()), c.WantTags(flag));
			storeClient = client;
			AddAll(storeClient.clientBuyingIdList ?? (storeClient.clientBuyingIdList = new List<string>()), c.WantIds(flag));
			storeClient = client;
			AddAll(storeClient.clientBlackIdList ?? (storeClient.clientBlackIdList = new List<string>()), c.RefuseIds);
		}
		client.CompleteClientCreation();
		if (v.Buy || v.Both)
		{
			client.SetBudget(Settings.AgentBudget);
			client.willAcceptItem = (flag ? new Func<GameItem, bool>(c.DailyAccepts) : new Func<GameItem, bool>(c.Accepts));
			client.placedWrongItemWhenSellingToDialogue = new Dialogue().SetText(c.Name, v.Wrong);
		}
		if (!v.TalkOnly && !v.Buy)
		{
			client.SetWholesale(v.Discount);
		}
		Dialogue dialogue = new Dialogue().SetText(c.Name, v.Lines[0]);
		Dialogue dialogue2 = dialogue;
		for (int i = 1; i < v.Lines.Length; i++)
		{
			dialogue2 = dialogue2.NextDialogue().SetText(c.Name, v.Lines[i]);
		}
		var visitSession = new CharacterVisitSession(c.Id, s, client);
		bool done = false;
		Action startTrade = () =>
		{
			if (!visitSession.IsCurrent || done) return;
			if (!done)
			{
				done = true;
				try
				{
					if (!PlaceVisitGifts(c, v.Gift, visitSession)) { done = false; return; }
				}
				catch (Exception error)
				{
					done = false;
					WarnVisit("赠品登记失败，保留人物预约：" + error.Message);
					return;
				}
				try
				{
					long num2 = 0L;
					string[] gift = v.Stock;
					foreach (string id2 in gift)
					{
						if (!visitSession.IsCurrent) return;
						num2 += Place(c, id2, owned: false);
					}
					if (!visitSession.IsCurrent) return;
					if (v.Extra.Length != 0)
					{
						TopUp(c, v.Extra, num2, v.Stock.Length);
					}
					if (!visitSession.IsCurrent) return;
					if (v.Buy || v.Both)
					{
						BuyActive = true;
					}
				}
				catch (Exception ex)
				{
					WarnVisit("摆货失败：" + ex.Message);
				}
				if (!visitSession.IsCurrent) return;
				MarkVisited(c, kind);
				try
				{
					if (kind.StartsWith("ch"))
					{
						Services.OnChapterDone(c.Id);
					}
				}
				catch (Exception ex2)
				{
					WarnVisit("公司记录失败：" + ex2.Message);
				}
				try
				{
					Services.MaybeGiveCard(c.Id);
				}
				catch (Exception ex3)
				{
					WarnVisit("名片失败：" + ex3.Message);
				}
			}
			if (visitSession.IsCurrent && !v.TalkOnly)
			{
				client.OnIntroduced();
			}
		};
		Action endAction = () =>
		{
			startTrade();
			if (!done || !visitSession.TryFinish()) return;
			try
			{
				UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
			}
			catch (Exception ex)
			{
				WarnVisit("离开失败：" + ex.Message);
			}
		};
		bool flag2 = !string.IsNullOrEmpty(v.Ask) && v.Choices.Length != 0;
		if (flag2)
		{
			Dialogue dialogue3 = dialogue2.NextDialogue().SetText(c.Name, v.Ask);
			bool choiceMade = false;
			Choice[] choices = v.Choices;
			foreach (Choice choice in choices)
			{
				Action then = choice.Then;
				Dialogue dialogue4 = new Dialogue().SetText("PLAYER", choice.Label);
				Dialogue dialogue5 = dialogue4.NextDialogue().SetText(c.Name, choice.Reply);
				if (v.TalkOnly)
				{
					dialogue5.NextDialogue().SetText(c.Name, v.Exit).SetEndAction(endAction);
				}
				else
				{
					dialogue5.SetEndAction(() =>
					{
						startTrade();
					});
				}
				dialogue3.AddChoice(Compat.ChoiceIcon(), choice.Label, "", dialogue4, () =>
				{
					if (!visitSession.IsCurrent || choiceMade || done) return;
					choiceMade = true;
					try
					{
						then?.Invoke();
						LogVisit(c.Name + "：玩家选了“" + choice.Label + "”");
					}
					catch (Exception ex)
					{
						WarnVisit("选项处理失败：" + ex.Message);
					}
				});
			}
		}
		else
		{
			dialogue2.SetEndAction(startTrade);
		}
		if (v.TalkOnly && !flag2)
		{
			dialogue2.NextDialogue().SetText(c.Name, v.Exit).SetEndAction(endAction);
		}
		client.mainDialogue = dialogue;
		if (!v.Both)
		{
			client.acceptDealDialogue = Exit(c, v.Exit, visitSession);
		}
		client.allDoneDialogue = Exit(c, v.Exit, visitSession);
		return client;
	}


	private static Dialogue Exit(Character c, string line, CharacterVisitSession visitSession)
	{
		return new Dialogue().SetText(c.Name, line).SetEndAction(() =>
		{
			if (!visitSession.TryFinish()) return;
			BuyActive = false;
			try
			{
				SaveSlot.CurrentStore.RemoveNotOwnedItemFromTable();
			}
			catch (Exception ex)
			{
				WarnVisit("收回货物失败：" + ex.Message);
			}
			try
			{
				Services.MaybeGiveCard(c.Id);
			}
			catch (Exception ex2)
			{
				WarnVisit("名片失败：" + ex2.Message);
			}
			try
			{
				UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
			}
			catch (Exception ex3)
			{
				WarnVisit("离开失败：" + ex3.Message);
			}
		});
	}


	private static void MarkVisited(Character c, string kind)
	{
		CharacterState characterState = StateOf(c);
		if (characterState == null)
		{
			return;
		}
		if (kind.StartsWith("ch") && int.TryParse(kind.Substring(2), out var result))
		{
			if (c.IsSide(result))
			{
				if (!characterState.SideDone.Contains(result))
				{
					characterState.SideDone.Add(result);
				}
			}
			else if (result > characterState.Chapter)
			{
				characterState.Chapter = result;
			}
			characterState.LastStoryDay = StoreStation.GetDayCounter();
		}
		if (kind.StartsWith("daily"))
		{
			characterState.DailyCount++;
		}
		characterState.LastVisitDay = StoreStation.GetDayCounter();
		if (c.HasDaily && characterState.Chapter >= 1)
		{
			characterState.NextDailyDay = characterState.LastVisitDay + c.NextDailyGap();
		}
		if (characterState.PendingKind == kind)
		{
			characterState.PendingDay = -1;
			characterState.PendingKind = null;
		}
		LogVisit($"{c.Name}来访完成：{kind}，进度第 {characterState.Chapter} 章");
	}


	private static void AddAll(List<string> list, string[] values)
	{
		foreach (string item in values)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
	}

}


