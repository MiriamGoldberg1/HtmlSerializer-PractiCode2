namespace HtmlSerializer
{
    /// <summary>
    /// פונקציות הרחבה (Extension Methods) לתשאול עץ HtmlElement לפי סלקטור.
    /// </summary>
    public static class HtmlElementExtensions
    {
        /// <summary>
        /// מחזירה את כל האלמנטים בעץ (מתחת לאלמנט הנוכחי) שעונים לסלקטור.
        /// </summary>
        public static IEnumerable<HtmlElement> FindElements(this HtmlElement element, Selector selector)
        {
            // HashSet מונע כפילויות: אלמנט שעונה לתנאי כמה פעמים
            // (למשל דרך הסבא וגם דרך האב) יופיע בתוצאה פעם אחת בלבד
            var result = new HashSet<HtmlElement>();
            FindElementsRecursive(element, selector, result);
            return result;
        }

        /// <summary>
        /// עומס נוח: מקבל את השאילתה כמחרוזת וממיר אותה ל-Selector.
        /// </summary>
        public static IEnumerable<HtmlElement> Query(this HtmlElement element, string query) =>
            element.FindElements(Selector.Parse(query));

        /// <summary>
        /// הפונקציה הריקורסיבית: רצה במקביל על עץ האלמנטים ועל שרשרת הסלקטורים.
        /// עומק הריקורסיה כאן הוא כמספר הרמות בשאילתה (קטן מאוד), ולכן אין סכנת StackOverflow.
        /// </summary>
        private static void FindElementsRecursive(HtmlElement element, Selector selector, HashSet<HtmlElement> result)
        {
            // כל הצאצאים של האלמנט הנוכחי, ללא האלמנט עצמו
            // (Descendants מחזירה גם את this, ולא רוצים שאלמנט יתאים לשתי רמות של השאילתה בו-זמנית)
            var matches = element.Descendants()
                                 .Where(d => d != element && d.Matches(selector));

            // תנאי עצירה: הגענו לסלקטור האחרון - האלמנטים המסוננים הם התוצאה
            if (selector.Child == null)
            {
                result.UnionWith(matches);
                return;
            }

            // אחרת - ממשיכים לחפש את הרמה הבאה של הסלקטור בתוך כל אחד מהאלמנטים שנמצאו
            foreach (HtmlElement match in matches)
            {
                FindElementsRecursive(match, selector.Child, result);
            }
        }
    }
}
