using System;
using CorpPresence.UI;

namespace CorpPresence;

internal static partial class Services
{
	public static void MaybeGiveCard(string charId)
	{
		var store = SaveSlot.CurrentStore;
		var session = StoreSession.Capture();
		if (StoreSession.CompanyProgressPaused || !session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
		Company company = Companies.ByCharacter(charId);
		CompanyState companyState = ((company == null) ? null : Companies.StateOf(company));
		if (companyState == null || companyState.CardGiven || companyState.Coop < 20)
		{
			return;
		}
		GameItem card = null;
		bool placed = false;
		try
		{
			card = Cards.Make(company);
			if (card == null)
			{
				ModLog.Warn(company.Name + "的名片没生成出来，下次再给");
				return;
			}
			if (StoreSession.CompanyProgressPaused || !session.IsCurrent || !SaveSlot.CanWrite("CorpPresence"))
			{
				MaintenanceDiagnostics.RecordRuntime("card", "session", "expired", null, card.identifier);
				return;
			}
			placed = store.TryAddDirectSellingItemToTable(card, isOwend: true);
			if (!placed)
			{
				MaintenanceDiagnostics.Record("card", "placement", "counter_rejected", null, card.identifier);
				ModLog.Info("柜台放满了，" + company.Name + "的名片明天由跑腿送来");
				return;
			}
		}
		catch (Exception ex)
		{
			MaintenanceDiagnostics.Record("card", card == null ? "create" : "placement", "exception", ex, card?.identifier);
			ModLog.Warn("名片生成失败，下次再给：" + ex.Message);
			return;
		}
		finally
		{
			if (!placed && card != null && card.parentInventory == null && !StorePlacement.Destroyed(card))
			{
				try { card.Destroy(); }
				catch (Exception error) { MaintenanceDiagnostics.Record("card", "cleanup", "discard_failed", error, card.identifier); }
			}
		}
		if (StoreSession.CompanyProgressPaused || !session.IsCurrent || !SaveSlot.CanWrite("CorpPresence"))
		{
			MaintenanceDiagnostics.RecordRuntime("card", "session", "expired", null, card.identifier);
			return;
		}
		companyState.CardGiven = true;
		Phone.Activate(company);
		try
		{
			Notebook.Refresh();
		}
		catch
		{
		}
		ModLog.Info($"拿到{company.Name}的名片，电话 {company.PhoneNumber}");
	}
}
