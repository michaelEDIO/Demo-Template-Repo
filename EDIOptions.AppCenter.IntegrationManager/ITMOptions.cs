using EDIOptions.AppCenter.Database;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    public class ITMOptions
    {
        public static string[] Columns =
        {
            _Column.Sh_PlAll,
            _Column.Sh_PlPp,
            _Column.Sh_PlMx,
            _Column.Sh_PlDs,
            _Column.BoxOptn,
            _Column.Sh_Inv
        };

        public bool IsPackingEnabled = false;
        public bool IsInvoiceEnabled = false;
        public bool IsPPEnabled = false;
        public bool IsMXEnabled = false;
        public bool IsDSEnabled = false;
        public string BoxOption = "1";

        public ITMOptions()
        {
        }

        public ITMOptions(DBResult queryPL)
        {
            IsPackingEnabled = queryPL.FieldByName(_Column.Sh_PlAll) != _SqlValue.False;
            IsInvoiceEnabled = queryPL.FieldByName(_Column.Sh_Inv) != _SqlValue.False;
            IsPPEnabled = queryPL.FieldByName(_Column.Sh_PlPp) != _SqlValue.False;
            IsMXEnabled = queryPL.FieldByName(_Column.Sh_PlMx) != _SqlValue.False;
            IsDSEnabled = queryPL.FieldByName(_Column.Sh_PlDs) != _SqlValue.False;
            switch (queryPL.FieldByName(_Column.BoxOptn, "7"))
            {
                case "1":
                    BoxOption = _CartonDistType.PerPO;
                    break;

                case "3":
                    BoxOption = _CartonDistType.PerPOStore;
                    break;

                case "4":
                case "6":
                    BoxOption = _CartonDistType.Manual;
                    break;

                case "5":
                    BoxOption = _CartonDistType.PerASN;
                    break;

                default:
                case "7":
                    BoxOption = _CartonDistType.Manual;
                    break;
            }
        }

        public List<string> GetPLOptions()
        {
            List<string> plTypes = new List<string>();
            if (IsPackingEnabled)
            {
                if (IsPPEnabled)
                {
                    plTypes.Add(_PackingListType.PrePacked);
                }
                if (IsMXEnabled)
                {
                    plTypes.Add(_PackingListType.Mixed);
                }
                if (IsDSEnabled)
                {
                    plTypes.Add(_PackingListType.Distributed);
                }
            }
            return plTypes;
        }

        public static List<string> GetCartonOptions(string plType)
        {
            switch (plType)
            {
                case _PackingListType.PrePacked:
                default:
                    return new List<string>()
                    {
                        _CartonDistType.Manual,
                        _CartonDistType.Automatic
                    };

                case _PackingListType.Mixed:
                    return new List<string>()
                    {
                        _CartonDistType.Manual,
                        _CartonDistType.PerASN,
                        _CartonDistType.PerPO
                    };

                case _PackingListType.Distributed:
                    return new List<string>()
                    {
                        _CartonDistType.PerPOStore
                    };
            }
        }
    }
}