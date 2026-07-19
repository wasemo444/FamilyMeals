namespace ManageFamilyMeals.Shared.Resources;

public static class LocalizationCatalog
{
    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppTitle"] = "Manage Family Meals",
            ["Home"] = "Home",
            ["Archive"] = "Archive",
            ["FavoriteCategories"] = "Favorite Categories",
            ["AllCategories"] = "All Categories",
            ["AddCategory"] = "Add Category",
            ["CategoryName"] = "Category name",
            ["Create"] = "Create",
            ["Delete"] = "Delete",
            ["Restore"] = "Restore",
            ["Favorite"] = "Favorite",
            ["Unfavorite"] = "Unfavorite",
            ["Open"] = "Open",
            ["Back"] = "Back",
            ["FavoriteLinks"] = "Favorite Links",
            ["AllLinks"] = "All Links",
            ["AddLink"] = "Add Link",
            ["LinkTitleEn"] = "Title (English, optional)",
            ["LinkTitleAr"] = "Title (Arabic, optional)",
            ["SearchLinks"] = "Search links by title or URL",
            ["NoSearchResults"] = "No links match your search.",
            ["LinkTitle"] = "Title (optional)",
            ["LinkUrl"] = "URL",
            ["LinkNote"] = "Note (optional)",
            ["LoadingPreview"] = "Loading preview...",
            ["PreviewUnavailable"] = "Preview unavailable",
            ["ArchivedItems"] = "Archived Items",
            ["ArchivedCategories"] = "Archived Categories",
            ["ArchivedLinks"] = "Archived Links",
            ["ArchiveRetentionNotice"] = "Items stay in archive for 7 days before permanent deletion.",
            ["NoCategoriesYet"] = "No categories yet. Create your first meal category.",
            ["NoLinksYet"] = "No links yet. Add your first link.",
            ["NoArchivedItems"] = "No archived items.",
            ["CategoryNotFound"] = "Category not found.",
            ["LanguageEnglish"] = "English",
            ["LanguageArabic"] = "Arabic",
            ["LinksCount"] = "{0} links",
            ["InvalidUrl"] = "Please enter a valid URL.",
            ["CategoryNameRequired"] = "Category name is required.",
            ["CategoryNameDuplicate"] = "A category with this name already exists.",
            ["SearchCategories"] = "Search categories by name",
            ["NoCategorySearchResults"] = "No categories match your search.",
            ["ShareLinkTitle"] = "Save shared link",
            ["SelectCategory"] = "Select category",
            ["SaveLink"] = "Save link",
            ["ShareNoCategories"] = "Create a category on the home page first.",
            ["LinkSaved"] = "Link saved.",
            ["ShareHint"] = "Choose a category and save the link below.",
            ["ShareCreateCategoryHint"] = "Create a category below to save this link.",
            ["QuickCreateCategory"] = "Quick create category",
            ["SelectCategoryRequired"] = "Please select a category.",
        },
        ["ar"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AppTitle"] = "إدارة وجبات العائلة",
            ["Home"] = "الرئيسية",
            ["Archive"] = "الأرشيف",
            ["FavoriteCategories"] = "الفئات المفضلة",
            ["AllCategories"] = "كل الفئات",
            ["AddCategory"] = "إضافة فئة",
            ["CategoryName"] = "اسم الفئة",
            ["Create"] = "إنشاء",
            ["Delete"] = "حذف",
            ["Restore"] = "استعادة",
            ["Favorite"] = "تفضيل",
            ["Unfavorite"] = "إلغاء التفضيل",
            ["Open"] = "فتح",
            ["Back"] = "رجوع",
            ["FavoriteLinks"] = "الروابط المفضلة",
            ["AllLinks"] = "كل الروابط",
            ["AddLink"] = "إضافة رابط",
            ["LinkTitleEn"] = "العنوان (إنجليزي، اختياري)",
            ["LinkTitleAr"] = "العنوان (عربي، اختياري)",
            ["SearchLinks"] = "ابحث في الروابط بالعنوان أو الرابط",
            ["NoSearchResults"] = "لا توجد روابط مطابقة لبحثك.",
            ["LinkTitle"] = "العنوان (اختياري)",
            ["LinkUrl"] = "الرابط",
            ["LinkNote"] = "ملاحظة (اختياري)",
            ["LoadingPreview"] = "جاري تحميل المعاينة...",
            ["PreviewUnavailable"] = "المعاينة غير متاحة",
            ["ArchivedItems"] = "العناصر المؤرشفة",
            ["ArchivedCategories"] = "الفئات المؤرشفة",
            ["ArchivedLinks"] = "الروابط المؤرشفة",
            ["ArchiveRetentionNotice"] = "تبقى العناصر في الأرشيف لمدة 7 أيام قبل الحذف النهائي.",
            ["NoCategoriesYet"] = "لا توجد فئات بعد. أنشئ أول فئة وجبات.",
            ["NoLinksYet"] = "لا توجد روابط بعد. أضف أول رابط.",
            ["NoArchivedItems"] = "لا توجد عناصر مؤرشفة.",
            ["CategoryNotFound"] = "الفئة غير موجودة.",
            ["LanguageEnglish"] = "الإنجليزية",
            ["LanguageArabic"] = "العربية",
            ["LinksCount"] = "{0} روابط",
            ["InvalidUrl"] = "يرجى إدخال رابط صالح.",
            ["CategoryNameRequired"] = "اسم الفئة مطلوب.",
            ["CategoryNameDuplicate"] = "يوجد فئة بهذا الاسم بالفعل.",
            ["SearchCategories"] = "ابحث في الفئات بالاسم",
            ["NoCategorySearchResults"] = "لا توجد فئات مطابقة لبحثك.",
            ["ShareLinkTitle"] = "حفظ الرابط المشارك",
            ["SelectCategory"] = "اختر الفئة",
            ["SaveLink"] = "حفظ الرابط",
            ["ShareNoCategories"] = "أنشئ فئة من الصفحة الرئيسية أولاً.",
            ["LinkSaved"] = "تم حفظ الرابط.",
            ["ShareHint"] = "اختر فئة واحفظ الرابط أدناه.",
            ["ShareCreateCategoryHint"] = "أنشئ فئة أدناه لحفظ هذا الرابط.",
            ["QuickCreateCategory"] = "إنشاء فئة سريع",
            ["SelectCategoryRequired"] = "يرجى اختيار فئة.",
        },
    };

    public static string Get(string cultureCode, string key)
    {
        var culture = NormalizeCulture(cultureCode);
        if (Strings.TryGetValue(culture, out var cultureStrings)
            && cultureStrings.TryGetValue(key, out var value))
        {
            return value;
        }

        if (Strings["en"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public static string Format(string cultureCode, string key, params object[] arguments)
    {
        var format = Get(cultureCode, key);
        return string.Format(System.Globalization.CultureInfo.GetCultureInfo(NormalizeCulture(cultureCode)), format, arguments);
    }

    private static string NormalizeCulture(string cultureCode) =>
        cultureCode.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
}
