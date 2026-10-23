using System.Globalization;

namespace VoltaXApi.Services.Billing
{
    /// <summary>
    /// Wording of the billing documents and emails in French (default), English and Arabic. A missing key falls
    /// back to French, then to the key itself, so a document always renders.
    /// </summary>
    public static class BillingTexts
    {
        public const string French = "fr";
        public const string English = "en";
        public const string Arabic = "ar";
        public static readonly IReadOnlyList<string> Languages = new[] { French, English, Arabic };

        /// <summary>fr, en or ar from a preference such as "en-US" or "AR"; anything else gives <paramref name="fallback"/>.</summary>
        public static string Normalize(string? language, string fallback = French)
        {
            var code = (language ?? "").Trim().ToLowerInvariant();
            if (code.Length > 2) code = code[..2];
            return Languages.Contains(code) ? code : Languages.Contains(fallback) ? fallback : French;
        }

        public static bool IsRightToLeft(string language) => language == Arabic;

        /// <summary>Numbers and amounts: Western digits everywhere (as on Moroccan documents), comma decimals in fr / ar.</summary>
        public static CultureInfo NumberCulture(string language) =>
            CultureInfo.GetCultureInfo(language == English ? "en-GB" : "fr-FR");

        public static string Money(decimal amount, string language, string currency = "MAD") =>
            $"{amount.ToString("N2", NumberCulture(language))} {currency}";

        public static string Get(string language, string key) =>
            Texts.TryGetValue(key, out var byLanguage)
                ? byLanguage.TryGetValue(language, out var text) ? text : byLanguage.GetValueOrDefault(French, key)
                : key;

        public static string Format(string language, string key, params object[] args) =>
            string.Format(NumberCulture(language), Get(language, key), args);

        private static Dictionary<string, string> T(string fr, string en, string ar) => new() { [French] = fr, [English] = en, [Arabic] = ar };

        private static readonly Dictionary<string, Dictionary<string, string>> Texts = new()
        {
            // Documents
            ["Invoice"] = T("FACTURE", "INVOICE", "فاتورة"),
            ["CreditNote"] = T("AVOIR", "CREDIT NOTE", "إشعار دائن"),
            ["Number"] = T("N°", "No.", "رقم"),
            ["IssuedOn"] = T("Date d'émission", "Issue date", "تاريخ الإصدار"),
            ["Seller"] = T("ÉMETTEUR", "SELLER", "المصدر"),
            ["BilledTo"] = T("FACTURÉ À", "BILLED TO", "فاتورة باسم"),
            ["CreditedTo"] = T("BÉNÉFICIAIRE", "CREDITED TO", "لفائدة"),
            ["Card"] = T("Carte", "Card", "البطاقة"),
            ["Email"] = T("E-mail", "Email", "البريد الإلكتروني"),
            ["Phone"] = T("Tél.", "Phone", "الهاتف"),
            ["Description"] = T("DÉSIGNATION", "DESCRIPTION", "البيان"),
            ["Quantity"] = T("QUANTITÉ", "QUANTITY", "الكمية"),
            ["UnitPrice"] = T("PRIX UNITAIRE TTC", "UNIT PRICE INCL. VAT", "سعر الوحدة مع الرسوم"),
            ["Amount"] = T("MONTANT TTC", "AMOUNT INCL. VAT", "المبلغ مع الرسوم"),
            ["EnergyLine"] = T("Énergie délivrée", "Energy delivered", "الطاقة المزودة"),
            ["TimeLine"] = T("Temps de charge", "Charging time", "مدة الشحن"),
            ["SessionFeeLine"] = T("Frais de session", "Session fee", "رسوم الجلسة"),
            ["IdleLine"] = T("Occupation après la charge", "Idle time after charging", "مدة الوقوف بعد الشحن"),
            ["IdleSupplementLine"] = T("Occupation après la charge (complément de la facture {0})", "Idle time after charging (supplement to invoice {0})", "مدة الوقوف بعد الشحن (تكملة للفاتورة {0})"),
            ["LegacyTimeLine"] = T("Charge (tarif au temps)", "Charging (time-based price)", "الشحن (تسعيرة حسب المدة)"),
            ["TopUpLine"] = T("Rechargement du portefeuille EVEON", "EVEON wallet top-up", "تعبئة محفظة EVEON"),
            ["RefundLine"] = T("Remboursement", "Refund", "استرداد"),
            ["Session"] = T("Session de charge", "Charging session", "جلسة الشحن"),
            ["Station"] = T("Station", "Station", "المحطة"),
            ["ChargePoint"] = T("Borne", "Charge point", "نقطة الشحن"),
            ["Period"] = T("Période", "Period", "الفترة"),
            ["Order"] = T("Commande", "Order", "الطلب"),
            ["VatSummary"] = T("Récapitulatif TVA", "VAT summary", "ملخص الضريبة على القيمة المضافة"),
            ["VatRate"] = T("TAUX", "RATE", "النسبة"),
            ["BaseExclVat"] = T("BASE HT", "NET AMOUNT", "المبلغ دون رسوم"),
            ["Vat"] = T("TVA", "VAT", "الضريبة"),
            ["TotalExclVat"] = T("Total HT", "Total excl. VAT", "المجموع دون رسوم"),
            ["TotalVat"] = T("TVA", "VAT", "الضريبة على القيمة المضافة"),
            ["TotalInclVat"] = T("Total TTC", "Total incl. VAT", "المجموع مع الرسوم"),
            ["TotalCredited"] = T("Total remboursé TTC", "Total refunded incl. VAT", "مجموع المبلغ المسترد"),
            ["Payment"] = T("RÈGLEMENT", "PAYMENT", "الأداء"),
            ["PaidByWallet"] = T("Débité du portefeuille EVEON (carte de recharge)", "Debited from the EVEON wallet (charging card)", "مقتطع من محفظة EVEON (بطاقة الشحن)"),
            ["PaidOnline"] = T("Payé en ligne ({0})", "Paid online ({0})", "مؤدى عبر الإنترنت ({0})"),
            ["PaidManual"] = T("Encaissé par un opérateur EVEON", "Collected by an EVEON operator", "محصل من طرف مشغل EVEON"),
            ["CreditsInvoice"] = T("Avoir sur la facture {0}", "Credit note for invoice {0}", "إشعار دائن للفاتورة {0}"),
            ["Reason"] = T("Motif", "Reason", "السبب"),
            ["RefundToWallet"] = T("Crédité sur le portefeuille EVEON", "Credited to the EVEON wallet", "أضيف إلى محفظة EVEON"),
            ["RefundToGateway"] = T("Remboursé sur le moyen de paiement d'origine ({0})", "Refunded to the original payment method ({0})", "أعيد إلى وسيلة الأداء الأصلية ({0})"),
            ["RefundExternal"] = T("Remboursé hors plateforme (virement ou espèces)", "Refunded outside the platform (transfer or cash)", "أعيد خارج المنصة (تحويل أو نقدا)"),
            ["PricesNote"] = T("Montants en dirhams marocains (MAD), TVA comprise.", "Amounts in Moroccan dirhams (MAD), VAT included.", "المبالغ بالدرهم المغربي، شاملة للضريبة على القيمة المضافة."),
            ["ThankYou"] = T("Merci d'avoir rechargé avec EVEON.", "Thank you for charging with EVEON.", "شكرا لشحنكم مع EVEON."),
            ["Page"] = T("Page", "Page", "صفحة"),
            ["Minutes"] = T("min", "min", "دقيقة"),
            ["PerKwh"] = T("/ kWh", "/ kWh", "/ كيلوواط ساعة"),
            ["PerMinute"] = T("/ min", "/ min", "/ دقيقة"),
            ["Connector"] = T("Connecteur", "Connector", "الموصل"),

            // Partner statements
            ["Statement"] = T("RELEVÉ PARTENAIRE", "PARTNER STATEMENT", "كشف الشريك"),
            ["Sessions"] = T("Sessions", "Sessions", "الجلسات"),
            ["Energy"] = T("Énergie", "Energy", "الطاقة"),
            ["Gross"] = T("Chiffre d'affaires TTC", "Gross revenue incl. VAT", "رقم المعاملات مع الرسوم"),
            ["NetExclVat"] = T("Chiffre d'affaires HT", "Net revenue excl. VAT", "رقم المعاملات دون رسوم"),
            ["Commission"] = T("Commission HT", "Commission excl. VAT", "العمولة دون رسوم"),
            ["CommissionVat"] = T("TVA sur commission", "VAT on commission", "الضريبة على العمولة"),
            ["NetPayout"] = T("Net à reverser", "Net payout", "الصافي المستحق"),
            ["Status"] = T("Statut", "Status", "الحالة"),
            ["Pending"] = T("En attente", "Pending", "في الانتظار"),
            ["Paid"] = T("Payé", "Paid", "مؤدى"),
            ["PaidOn"] = T("Payé le {0} (réf. {1})", "Paid on {0} (ref. {1})", "أدي بتاريخ {0} (مرجع {1})"),
            ["CommissionTerms"] = T("Commission : {0} % du HT + {1} MAD HT / kWh", "Commission: {0}% of net revenue + {1} MAD excl. VAT / kWh", "العمولة: {0}% من المبلغ دون رسوم + {1} درهم / كيلوواط ساعة"),
            ["Date"] = T("DATE", "DATE", "التاريخ"),
            ["RefundAdjustment"] = T("Remboursement session #{0}", "Refund of session #{0}", "استرداد الجلسة #{0}"),

            // Emails
            ["MailGreeting"] = T("Bonjour {0},", "Hello {0},", "مرحبا {0}،"),
            ["MailSignature"] = T("L'équipe EVEON", "The EVEON team", "فريق EVEON"),
            ["MailTopUpSubject"] = T("Votre rechargement de {0} – facture {1}", "Your {0} top-up – invoice {1}", "تعبئة بمبلغ {0} – الفاتورة {1}"),
            ["MailTopUpBody"] = T("Votre portefeuille EVEON a été crédité de {0}. Vous trouverez la facture {1} en pièce jointe.",
                "Your EVEON wallet was credited with {0}. Invoice {1} is attached.",
                "تمت تعبئة محفظة EVEON الخاصة بكم بمبلغ {0}. تجدون الفاتورة {1} مرفقة."),
            ["MailRefundSubject"] = T("Remboursement de {0} – avoir {1}", "Refund of {0} – credit note {1}", "استرداد مبلغ {0} – الإشعار الدائن {1}"),
            ["MailRefundWalletBody"] = T("Nous vous avons remboursé {0} sur votre portefeuille EVEON ({1}). L'avoir {2} est en pièce jointe.",
                "We refunded {0} to your EVEON wallet ({1}). Credit note {2} is attached.",
                "قمنا بإرجاع {0} إلى محفظة EVEON الخاصة بكم ({1}). الإشعار الدائن {2} مرفق."),
            ["MailRefundCardBody"] = T("Nous vous avons remboursé {0} sur votre moyen de paiement ({1}). Le montant a été retiré de votre portefeuille EVEON. L'avoir {2} est en pièce jointe.",
                "We refunded {0} to your payment method ({1}). The amount was taken off your EVEON wallet. Credit note {2} is attached.",
                "قمنا بإرجاع {0} إلى وسيلة الأداء الخاصة بكم ({1}). تم خصم المبلغ من محفظة EVEON. الإشعار الدائن {2} مرفق."),
            ["MailSessionSubject"] = T("Votre facture {0} – session de charge #{1}", "Your invoice {0} – charging session #{1}", "فاتورتكم {0} – جلسة الشحن #{1}"),
            ["MailSessionBody"] = T("Votre session de charge #{0} a été facturée {1}. La facture {2} est en pièce jointe.",
                "Your charging session #{0} was billed {1}. Invoice {2} is attached.",
                "تمت فوترة جلسة الشحن #{0} بمبلغ {1}. الفاتورة {2} مرفقة."),
            ["NotificationRefund"] = T("Remboursement de {0} : avoir {1}.", "Refund of {0}: credit note {1}.", "استرداد مبلغ {0}: الإشعار الدائن {1}."),
        };
    }
}
