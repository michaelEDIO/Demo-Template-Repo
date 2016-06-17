using EDIOptions.AppCenter.Database;

namespace EDIOptions.AppCenter.PoDropShip
{
    public class PdsOptions
    {
        public static string[] Columns =
        {
            _Column.Sh_PlAll,
            _Column.Sh_Inv
        };

        public readonly bool IsPackingEnabled = false;
        public readonly bool IsInvoiceEnabled = false;

        public PdsOptions()
        {
        }

        public PdsOptions(DBResult queryPL)
        {
            IsPackingEnabled = queryPL.FieldByName(_Column.Sh_PlAll) != _SqlValue.False;
            IsInvoiceEnabled = queryPL.FieldByName(_Column.Sh_Inv) != _SqlValue.False;
        }
    }
}