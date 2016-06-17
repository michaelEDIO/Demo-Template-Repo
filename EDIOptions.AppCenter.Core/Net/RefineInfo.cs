using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace EDIOptions.AppCenter
{
    /// <summary>
    /// Represents a refinement of table data.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RefineInfo
    {
        /// <summary>
        /// The displayed name of the column.
        /// </summary>
        [JsonProperty(PropertyName = "Header")]
        public string Header { get; set; } = "";

        /// <summary>
        /// The name of the column in the table.
        /// </summary>
        [JsonProperty(PropertyName = "Column")]
        public string Column { get; set; } = "";

        /// <summary>
        /// The selected index of the refined options.
        /// </summary>
        [JsonProperty(PropertyName = "SelectedIndex")]
        public int SelectedIndex { get; set; } = 0;

        /// <summary>
        /// The type of refinement.
        /// </summary>
        [JsonProperty(PropertyName = "Type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RefineType Type { get; set; } = RefineType.Select;

        [JsonProperty(PropertyName = "From")]
        public DateTime? From { get; set; } = DateTime.MinValue;

        [JsonProperty(PropertyName = "To")]
        public DateTime? To { get; set; } = DateTime.MinValue;

        [JsonProperty(PropertyName = "Min")]
        public decimal? Min { get; set; } = 0;

        [JsonProperty(PropertyName = "Max")]
        public decimal? Max { get; set; } = 0;

        /// <summary>
        /// The list of options to select from.
        /// </summary>
        [JsonProperty(PropertyName = "SelectOptions")]
        public List<string> SelectOptions { get; set; } = new List<string>();

        /// <summary>
        /// Constructs a new instance of the <see cref="RefineInfo"/> class.
        /// </summary>
        public RefineInfo()
        {
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="RefineInfo"/> class as a date refinement.
        /// </summary>
        /// <param name="header">The displayed name of the column.</param>
        /// <param name="column">The name of the column in the table.</param>
        /// <returns>A new instance of the <see cref="RefineInfo"/> class as a date refinement.</returns>
        public static RefineInfo CreateDateRefine(string header, string column) => new RefineInfo() { Header = header, Column = column, SelectedIndex = 0, Type = RefineType.Date };

        /// <summary>
        /// Constructs a new instance of the <see cref="RefineInfo"/> class as a number range refinement.
        /// </summary>
        /// <param name="header">The displayed name of the column.</param>
        /// <param name="column">The name of the column in the table.</param>
        /// <returns>A new instance of the <see cref="RefineInfo"/> class as a number range refinement.</returns>
        public static RefineInfo CreateRangeRefine(string header, string column) => new RefineInfo() { Header = header, Column = column, SelectedIndex = 0, Type = RefineType.Number };

        /// <summary>
        /// Returns the conditional equivalent of this refinement.
        /// </summary>
        /// <returns></returns>
        public string ToFilter()
        {
            string sColumn = Column.Trim().SQLEscape();
            if (string.IsNullOrEmpty(sColumn))
            {
                return "1";
            }
            if (SelectedIndex < 0)
            {
                SelectedIndex = 0;
            }
            switch (Type)
            {
                case RefineType.Date:
                    {
                        switch (SelectedIndex)
                        {
                            case 0:
                            default:
                                return "1";

                            case 1:
                                {
                                    if (From == null)
                                    {
                                        return "1";
                                    }
                                    var from = (DateTime)From;
                                    return $"`{sColumn}`='{from.ToString("yyyy-MM-dd")}'";
                                }

                            case 2:
                                {
                                    if (From == null || To == null)
                                    {
                                        return "1";
                                    }
                                    var from = (DateTime)From;
                                    var to = (DateTime)To;
                                    return $"`{sColumn}`>='{from.ToString("yyyy-MM-dd")}' AND `{sColumn}`<='{to.ToString("yyyy-MM-dd")}'";
                                }
                        }
                    }
                case RefineType.Number:
                    {
                        switch (SelectedIndex)
                        {
                            // 0: Any amount
                            // 1: Greater Than
                            // 2: Less Than
                            // 3: Equal To
                            // 4: Between
                            case 0:
                            default:
                                return "1";

                            case 1:
                                {
                                    if (Min == null)
                                    {
                                        return "1";
                                    }
                                    return $"`{sColumn}`>'{Min}'";
                                }

                            case 2:
                                {
                                    if (Min == null)
                                    {
                                        return "1";
                                    }
                                    return $"`{sColumn}`<'{Min}'";
                                }
                            case 3:
                                {
                                    if (Min == null)
                                    {
                                        return "1";
                                    }
                                    return $"`{sColumn}`='{Min}'";
                                }
                            case 4:
                                {
                                    if (Min == null || Max == null)
                                    {
                                        return "1";
                                    }
                                    return $"`{sColumn}`>='{Min}' AND `{sColumn}`<='{Max}'";
                                }
                        }
                    }
                case RefineType.Select:
                default:
                    {
                        if (SelectOptions == null || SelectedIndex >= SelectOptions.Count)
                        {
                            return "1";
                        }
                        string sValue = SelectOptions[SelectedIndex].Trim().SQLEscape();
                        switch (sColumn)
                        {
                            case "hprocessed":
                            case "processed":
                                {
                                    switch (sValue.ToLower())
                                    {
                                        case "processed":
                                            return sColumn + "='Y'";

                                        case "all":
                                            return "1";

                                        default:
                                        case "pending":
                                            return sColumn + "='S'";

                                        case "pending & errors":
                                            return "(" + sColumn + "='S' OR " + sColumn + "='X')";

                                        case "error":
                                            return sColumn + "='X'";
                                    }
                                }
                            case "xfertype":
                                {
                                    switch (sValue.ToLower())
                                    {
                                        default:
                                        case "all":
                                            return "1";

                                        case "distributed":
                                            return sColumn + "='D'";

                                        case "mixed":
                                            return sColumn + "='M'";

                                        case "prepacked":
                                            return sColumn + "='P'";

                                        case "consolidated":
                                            return sColumn + "='C'";
                                    }
                                }
                            default:
                                {
                                    return $"`{sColumn}`='{sValue}'";
                                }
                        }
                    }
            }
        }
    }
}