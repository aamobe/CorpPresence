using System;
namespace CorpPresence;
internal static partial class Services
{
    public static string Pay(Company c, int amount, string what)
    {
        PlayerStore instance = SaveSlot.CurrentStore;
        if (instance == null)
        {
            RecordPaymentFault("payment.check", "no_store");
            return L.K("companies.services.pay.saveNotReady", "存档未就绪", "Save not ready");
        }
        if (c != null)
        {
            CompanyState companyState = Companies.StateOf(c);
            if (companyState == null || companyState.Orders == null || companyState.Visitors == null || companyState.LimitedBought == null)
            {
                RecordPaymentFault("payment.check", "invalid_company");
                return L.K("companies.services.pay.companyDataNotReady", "公司数据未就绪，尚未付款。", "Company data is not ready. No payment attempted.");
            }
            if (Settings.MaliceMode) CorpPresence.Malice.MaliceState.Get();
            if (!SaveSlot.CanWrite("CorpPresence") || (Settings.MaliceMode && !SaveSlot.CanWrite("CorpPresence.Malice")))
            {
                RecordPaymentFault("payment.check", "protected_record");
                return L.K("companies.services.pay.saveLoadError", "公司数据未就绪，尚未付款。请先检查存档读取错误。", "Company data is not ready. No payment attempted. Check the save-load error first.");
            }
        }
        // Free services still consume items or create orders: protect their save data too.
        if (amount <= 0) return null;
        if (Malice.Malice.IsInstalled && c != null)
        {
            string text;
            try { text = Malice.Malice.Charge(c, amount, what); }
            catch (Exception error)
            {
                RecordPaymentFault("payment.extension", "uncertain", error);
                try { ModLog.Warn("公司付款处理失败，不再补扣现金：" + error.Message); } catch { }
                return L.K("companies.services.pay.paymentStatusUncertainCheckBalance", "付款状态异常，请先核对余额，避免重复付款。", "Payment status is uncertain. Check your balance before trying again.");
            }
            if (text != null)
            {
                if (!(text == ""))
                {
                    RecordPaymentFault("payment.extension", "refused");
                    return text;
                }
                return null;
            }
        }
        if (!instance.CanPay(amount))
        {
            RecordPaymentFault("payment.cash", "insufficient");
            return L.K("companies.services.pay.notEnoughCash", "现金不够", "Not enough cash");
        }
        // Vanilla debits first, then plays audio. Sound failure must not lose the order.
        CompanyMoney.Pay(instance, amount);
        return null;
    }

    private static void RecordPaymentFault(string stage, string code, Exception error = null)
    {
        try { MaintenanceDiagnostics.Record("services", stage, code, error); } catch { }
    }
}
