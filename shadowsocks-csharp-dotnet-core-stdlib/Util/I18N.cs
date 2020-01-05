using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

using NLog;

namespace Shadowsocks.Std.Util
{
    /// <summary>
    /// Get the I18N content
    /// </summary>
    public interface IGetI18N
    {
        public string GetContent();
    }

    internal class StdI18N : IGetI18N
    {
        public string GetContent() => Resources.i18n_csv;
    }

    internal class ExternalI18N : IGetI18N
    {
        public string GetContent()
        {
            if (!File.Exists(I18N.I18N_FILE))
                return null;
            else
                return File.ReadAllText(I18N.I18N_FILE, Encoding.UTF8);
        }
    }

    public static class I18N
    {
        public const string I18N_FILE = "i18n.csv";

        public static string locale = CultureInfo.CurrentCulture.Name;

        public static ISet<string> Languages { get; private set; } = new HashSet<string>();

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static readonly List<IGetI18N> resources = new List<IGetI18N>();

        private static readonly IDictionary<string, string> _strings = new Dictionary<string, string>();

        static I18N()
        {
            var i18n = new List<IGetI18N>(AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IGetI18N)))).Select(a => Activator.CreateInstance(a)).Cast<IGetI18N>())
            {
                new StdI18N(),
                new ExternalI18N()
            };

            Init(i18n);
        }

        private static void Init(List<IGetI18N> resources)
        {
            I18N.resources.AddRange(resources);

            Reload();
        }

        private static void Reload()
        {
            _strings.Clear();

            foreach (var resource in resources)
            {
                Analysis(resource.GetContent());
            }

            if (_strings.Keys.Count == 0)
                _logger.Warn($"Translation for {locale} not found.");
        }

        private static void Analysis(string content)
        {
            if (content == null) return;

            var dataTable = new DataTable();

            using var sr = new StringReader(content);
            using var csv = new CsvReader(sr);
            using var cdr = new CsvDataReader(csv);

            dataTable.Load(cdr);

            if (dataTable.Rows.Count > 1)
            {
                int enIndex = -1;
                int localeIndex = -1;

                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var localeName = dataTable.Columns[i].ToString();

                    if (localeName.Equals("en"))
                        enIndex = i;
                    if (localeName.Equals(locale))
                        localeIndex = i;
                }

                // Fallback to same language with different region
                if (localeIndex == -1)
                {
                    string localeNoRegion = locale.Split('-')[0];
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        if (dataTable.Columns[i].ToString().Split('-')[0] == localeNoRegion)
                            localeIndex = i;
                    }
                }

                // Read the content
                if (enIndex != -1 && localeIndex != -1 && enIndex != localeIndex && dataTable.Rows.Count > 0)
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        // line start with comment
                        if (row.ItemArray[0].ToString().TrimStart().StartsWith("#")) continue;

                        var source = row.ItemArray[enIndex].ToString();
                        var translation = row.ItemArray[localeIndex].ToString();

                        // source string or translation empty
                        if (source.IsNullOrWhiteSpace() || translation.IsNullOrWhiteSpace()) continue;

                        _strings[source] = translation;
                    }
                }
            }
        }

        public static string GetString(string key, params object[] args) => string.Format(_strings.TryGetValue(key.Trim(), out var value) ? value : key, args);
    }
}