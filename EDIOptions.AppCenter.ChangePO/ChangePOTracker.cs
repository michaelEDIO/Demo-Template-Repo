using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIOptions.AppCenter.ChangePO
{
    public static class ChangePOTracker
    {
        private static string[] list860Cols =
        {
            _Column.UniqueKey,
            _Column.UniqueItem,
            _Column.TrxDate,
            _Column.Purpose,
            _Column.PONumber,
            _Column.MarkForID,
            _Column.POLine,
            _Column.Quantity,
            _Column.ChangeQuantity,
            _Column.ChangeType,
            _Column.UnitMeasure,
            _Column.UnitPrice,
            _Column.PriceBasis,
            _Column.InPackSize,
            _Column.InPackUm,
            _Column.GTIN,
            _Column.UPCNum,
            _Column.VendorNum,
            _Column.BuyerNum,
            _Column.RetailPrice,
            _Column.ItemDesc,
            _Column.ItemColor,
            _Column.ItemSize,
            _Column.PackSize,
            _Column.DetailNotes,
            _Column.Dropship,
            _Column.RecvID,
            _Column.SendID,
            _Column.Partner
        };

        public static List<CPOSummaryHead> GetChangeList(User user)
        {
            List<CPOSummaryHead> ret = new List<CPOSummaryHead>();
            DBConnect connection = new DBConnect();
            try
            {
                connection.Connect(ConnectionsMgr.GetSHConnInfo(user, _Database.ECGB));
                var queryCPOHead = connection.Select(new[] { _Column.UniqueKey, _Column.PONumber, _Column.POChangeDate, _Column.Purpose, _Column.TotalItems, _Column.HProcessed }, _Table.Head860, string.Format("WHERE {0}='{1}' AND {2}='{3}' AND ({4}='{5}' OR {4}='{6}')", _Column.Customer, user.Customer, _Column.Partner, user.ActivePartner, _Column.HProcessed, _ProgressFlag.Unprocessed, _ProgressFlag.Error));

                while (queryCPOHead.Read())
                {
                    CPOSummaryHead newHead = new CPOSummaryHead();
                    newHead.UniqueKey = queryCPOHead.Field(0, "");
                    newHead.PONumber = queryCPOHead.Field(1, "");
                    newHead.POChangeDate = queryCPOHead.Field(2, "");
                    newHead.Purpose = ElementLookup.GetDesc(user, _Element.Purpose, queryCPOHead.Field(3, ""));
                    newHead.Affected = queryCPOHead.Field(4, "");
                    newHead.Status = queryCPOHead.Field(5, "");
                    newHead.Details = new List<CPOSummaryDetail>();
                    var queryCPODetail = connection.Select(new[] { _Column.ChangeType, _Column.Quantity, _Column.ChangeQuantity, _Column.UnitPrice, _Column.RetailPrice, _Column.UPCNum, _Column.VendorNum, _Column.ItemDesc, _Column.PackSize, _Column.Dropship, _Column.Processed }, _Table.Detail860, string.Format("WHERE {0}='{1}'", _Column.UniqueKey, newHead.UniqueKey));
                    while (queryCPODetail.Read())
                    {
                        CPOSummaryDetail newDetail = new CPOSummaryDetail();
                        newDetail.ChangeType = ElementLookup.GetDesc(user, _Element.ChangeType, queryCPODetail.Field(0, ""));
                        newDetail.Quantity = decimal.Parse(queryCPODetail.Field(1, "0")).ToString("N0");
                        newDetail.ChangeQuantity = decimal.Parse(queryCPODetail.Field(2, "0")).ToString("N0");
                        newDetail.UnitPrice = queryCPODetail.Field(3, "");
                        newDetail.RetailPrc = queryCPODetail.Field(4, "");
                        newDetail.UPC = queryCPODetail.Field(5, "");
                        newDetail.VendorNum = queryCPODetail.Field(6, "");
                        newDetail.ItemDesc = queryCPODetail.Field(7, "");
                        newDetail.PackSize = queryCPODetail.Field(8, "");
                        newDetail.Dropship = queryCPODetail.Field(9, "").Replace('\r', ' ');
                        newDetail.Status = queryCPODetail.Field(10, "");
                        newHead.Details.Add(newDetail);
                    }
                    ret.Add(newHead);
                }
                return ret.OrderBy(h => h.POChangeDate).ThenBy(h => h.PONumber).ThenBy(h => h.Purpose).ToList();
            }
            catch (Exception e)
            {
                connection.Close();
                ProgramLog.LogError(user, "ChangePOTracker", "GetChangeList", e.Message);
                return new List<CPOSummaryHead>();
            }
        }

        public static ResponseType PerformAction(User user, CPOAction action)
        {
            try
            {
                switch (action.Action)
                {
                    case ActionType.Cancel:
                        return Cancel860(user, action.Key.SQLEscape());

                    case ActionType.Apply:
                        return Apply860(user, action.Key.SQLEscape());

                    default:
                        return ResponseType.ErrorCPOUnknown;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "PerformAction", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        #region Action On 860s

        private static ResponseType Cancel860(User user, string sHeadUniqueKey860)
        {
            DBConnect connection = new DBConnect();
            try
            {
                connection.Connect(ConnectionsMgr.GetSHConnInfo(user, _Database.ECGB));
                UpdateAll860ProcFlags(connection, sHeadUniqueKey860, _ProgressFlag.Canceled);
                connection.Close();
                return ResponseType.SuccessCPO;
            }
            catch (Exception e)
            {
                connection.Close();
                ProgramLog.LogError(user, "ChangePOTracker", "Cancel860", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        private static ResponseType Apply860(User user, string sHeadUniqueKey860)
        {
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.ECGB);
                string filterUniqueKey = string.Format("WHERE {0}='{1}'", _Column.UniqueKey, sHeadUniqueKey860);
                var resultCheckHead = connection.Select(new[] { _Column.PONumber, _Column.Purpose }, _Table.Head860, filterUniqueKey);
                if (resultCheckHead.AffectedRows == 0)
                {
                    return ResponseType.ErrorCPOCouldNotApplyItemChange;
                }
                resultCheckHead.Read();
                string sPONumber = resultCheckHead.Field(0, "");
                string sPurpose = resultCheckHead.Field(1, "");

                ResponseType resultOperation = ResponseType.ErrorCPOUnknown;

                switch (sPurpose)
                {
                    case Code353.Cancel:
                        resultOperation = CancelPO(connection, user, sPONumber);
                        break;

                    case Code353.Change:
                        resultOperation = ChangePO(connection, user, sPONumber, sHeadUniqueKey860);
                        break;

                    default:
                        resultOperation = ResponseType.ErrorCPOPurposeUnrecognized;
                        break;
                }
                if (resultOperation == ResponseType.SuccessCPO)
                {
                    UpdateHead860ProcFlag(connection, sHeadUniqueKey860, _ProgressFlag.Success);
                }
                else
                {
                    UpdateHead860ProcFlag(connection, sHeadUniqueKey860, _ProgressFlag.Error);
                }
                return resultOperation;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "Apply860", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        #endregion Action On 860s

        #region PO Purpose Handling

        private static ResponseType CancelPO(DBConnect connection, User user, string sPONumber)
        {
            try
            {
                string filterPONumber = string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.Partner, user.ActivePartner.SQLEscape(), _Column.PONumber, sPONumber);

                var updateVals = new Dictionary<string, string>()
                {
                    {_Column.CancelDate, DateTime.Now.ToString("yyyy-MM-dd")}
                }.ToNameValueCollection();

                connection.Update(_Table.Head850All, updateVals, filterPONumber);

                return ResponseType.SuccessCPO;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "CancelPO", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        private static ResponseType ChangePO(DBConnect connection, User user, string sPONumber, string sHeadUniqueKey860)
        {
            string filterUniqueKey860 = string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.UniqueKey, sHeadUniqueKey860, _Column.Processed, _ProgressFlag.Unprocessed);

            var result860Detail = connection.Select(list860Cols, _Table.Detail860, filterUniqueKey860);
            ResponseType resultOperation = ResponseType.ErrorCPOUnknown;
            while (result860Detail.Read())
            {
                Dictionary<string, string> sVals860 = Get860ValDict(result860Detail);
                if (!sVals860.ContainsKey(_Column.PONumber))
                {
                    sVals860.Add(_Column.PONumber, sPONumber);
                }
                if (!sVals860.ContainsKey(_Column.ChangeType))
                {
                    resultOperation = ResponseType.ErrorCPOCouldNotApplyItemChange;
                    break;
                }
                var resUniqueKeyPO = connection.Select(_Column.UniqueKey, _Table.Head850All, string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.Partner, user.ActivePartner.SQLEscape(), _Column.PONumber, sVals860[_Column.PONumber]));
                resUniqueKeyPO.Read();
                string sHeadUniqueKey850 = resUniqueKeyPO.Field(0);

                string filterLineItem850 = GetFilterItem(user, sHeadUniqueKey850, sVals860);
                if (filterLineItem850 == "")
                {
                    resultOperation = ResponseType.ErrorCPOCouldNotApplyItemChange;
                    break;
                }
                switch (sVals860[_Column.ChangeType])
                {
                    case Code670.AddItem:
                        {
                            resultOperation = AddItem(connection, user, sHeadUniqueKey850, sVals860);
                            break;
                        }
                    case Code670.DeleteItem:
                        {
                            resultOperation = DeleteItem(connection, user, filterLineItem850);
                            break;
                        }
                    case Code670.PriceChange:
                        {
                            if (sVals860.ContainsKey(_Column.UnitPrice))
                            {
                                resultOperation = PriceChange(connection, user, sVals860[_Column.UnitPrice], filterLineItem850);
                            }
                            else
                            {
                                resultOperation = ResponseType.ErrorCPOMissingPrice;
                            }
                            break;
                        }
                    case Code670.QuantityIncrease:
                    case Code670.QuantityDecrease:
                        {
                            if (sVals860.ContainsKey(_Column.ChangeQuantity))
                            {
                                resultOperation = QuantityChange(connection, user, sVals860[_Column.ChangeQuantity], filterLineItem850);
                            }
                            else
                            {
                                resultOperation = ResponseType.ErrorCPOMissingQuantity;
                            }
                            break;
                        }
                    case Code670.ReplaceAllItems:
                    case Code670.ChangeItem:
                        {
                            resultOperation = ReplaceAllValues(connection, user, sVals860, filterLineItem850);
                            break;
                        }
                    case Code670.UnitPriceQuantityChange:
                        {
                            ResponseType resultPrice = ResponseType.ErrorCPOUnknown;
                            ResponseType resultQuantity = ResponseType.ErrorCPOUnknown;
                            bool prcChanged = false;
                            bool qtyChanged = false;
                            if (sVals860.ContainsKey(_Column.UnitPrice))
                            {
                                resultPrice = PriceChange(connection, user, sVals860[_Column.UnitPrice], filterLineItem850);
                                if (resultPrice == ResponseType.SuccessCPO)
                                {
                                    prcChanged = true;
                                }
                            }
                            if (sVals860.ContainsKey(_Column.ChangeQuantity))
                            {
                                resultQuantity = QuantityChange(connection, user, sVals860[_Column.ChangeQuantity], filterLineItem850);
                                if (resultQuantity == ResponseType.SuccessCPO || resultQuantity == ResponseType.ErrorCPOCouldNotApplyItemChange)
                                {
                                    qtyChanged = true;
                                }
                            }
                            if (prcChanged || qtyChanged)
                            {
                                resultOperation = ResponseType.SuccessCPO;
                            }
                            else
                            {
                                resultOperation = ResponseType.ErrorCPOCouldNotApplyItemChange;
                            }
                            break;
                        }
                    default:
                        {
                            resultOperation = ResponseType.ErrorCPOChangeUnrecognized;
                            break;
                        }
                }
                if (resultOperation == ResponseType.SuccessCPO)
                {
                    UpdateDetl860ProcFlag(connection, result860Detail.Field(0), result860Detail.Field(1), _ProgressFlag.Success);
                }
                else
                {
                    UpdateDetl860ProcFlag(connection, result860Detail.Field(0), result860Detail.Field(1), _ProgressFlag.Error);
                    break;
                }
            }
            return resultOperation;
        }

        #endregion PO Purpose Handling

        #region Change Type Handling

        private static ResponseType AddItem(DBConnect connection, User user, string sUniqueKeyPO, Dictionary<string, string> sVals860)
        {
            try
            {
                Dictionary<string, string> sInsertVals = GetInsert850Vals(connection, sUniqueKeyPO, sVals860);

                var result = connection.Insert(_Table.Detail850All, GetInsert850Vals(connection, sUniqueKeyPO, sVals860).ToNameValueCollection());

                if (result.AffectedRows > 0)
                {
                    return ResponseType.SuccessCPO;
                }
                else
                {
                    return ResponseType.ErrorCPOCouldNotApplyItemChange;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "AddItem", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        private static ResponseType DeleteItem(DBConnect connection, User user, string sFilterLineItem)
        {
            try
            {
                var result = connection.Delete(_Table.Detail850All, sFilterLineItem);

                if (result.AffectedRows > 0)
                {
                    return ResponseType.SuccessCPO;
                }
                else
                {
                    return ResponseType.ErrorCPOCouldNotApplyItemChange;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "DeleteItem", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        private static ResponseType ReplaceAllValues(DBConnect connection, User user, Dictionary<string, string> sVals860, string sFilterLineItem)
        {
            try
            {
                var result = connection.Update(_Table.Detail850All, GetUpdate850Vals(sVals860).ToNameValueCollection(), sFilterLineItem);

                if (result.AffectedRows > 0)
                {
                    return ResponseType.SuccessCPO;
                }
                else
                {
                    return ResponseType.ErrorCPOCouldNotApplyItemChange;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "ReplaceAllValues", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        private static ResponseType QuantityChange(DBConnect connection, User user, string sUpdateQuantity, string sFilterLineItem)
        {
            try
            {
                var sUpdateVals = new Dictionary<string, string>
                {
                    {_Column.Quantity, sUpdateQuantity}
                }.ToNameValueCollection();

                var result = connection.Update(_Table.Detail850All, sUpdateVals, sFilterLineItem);

                if (result.AffectedRows > 0)
                {
                    return ResponseType.SuccessCPO;
                }
                else
                {
                    return ResponseType.ErrorCPOCouldNotApplyItemChange;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "QuantityChange", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        private static ResponseType PriceChange(DBConnect connection, User user, string sUnitPrice, string sFilterLineItem)
        {
            try
            {
                var sUpdateVals = new Dictionary<string, string>()
                {
                    {_Column.UnitPrice, sUnitPrice.SQLEscape()}
                }.ToNameValueCollection();

                var result = connection.Update(_Table.Detail850All, sUpdateVals, sFilterLineItem);

                if (result.AffectedRows > 0)
                {
                    return ResponseType.SuccessCPO;
                }
                else
                {
                    return ResponseType.ErrorCPOCouldNotApplyItemChange;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "PriceChange", e.Message);
                return ResponseType.ErrorCPOUnknown;
            }
        }

        private static Dictionary<string, string> Get860ValDict(DBResult result860Query)
        {
            Dictionary<string, string> sValsFrom860 = new Dictionary<string, string>();
            for (int i = 0; i < list860Cols.Length; i++)
            {
                sValsFrom860.Add(list860Cols[i].SQLEscape(), result860Query.Field(i, "").Trim().SQLEscape());
            }
            return sValsFrom860;
        }

        private static string GetFilterItem(User user, string sHeadUniqueKey850, Dictionary<string, string> sVals860)
        {
            List<string> cond = new List<string>();
            if (sVals860.ContainsKey(_Column.UPCNum))
            {
                cond.Add(string.Format("{0}='{1}'", _Column.UPCNum, sVals860[_Column.UPCNum]));
            }
            if (sVals860.ContainsKey(_Column.BuyerNum))
            {
                cond.Add(string.Format("{0}='{1}'", _Column.BuyerNum, sVals860[_Column.BuyerNum]));
            }
            if (sVals860.ContainsKey(_Column.VendorNum))
            {
                cond.Add(string.Format("{0}='{1}'", _Column.VendorNum, sVals860[_Column.VendorNum]));
            }
            if (cond.Count == 0)
            {
                ProgramLog.LogError(user, "ChangePOTracker", "GetFilterItem", string.Format("Item under POC key {0} could not be found in original PO by upc/buyer/vendor.", sHeadUniqueKey850));
                return "";
            }
            else
            {
                return string.Format("WHERE {0}='{1}' AND ({2})", _Column.UniqueKey, sHeadUniqueKey850, string.Join(" OR ", cond));
            }
        }

        private static Dictionary<string, string> GetInsert850Vals(DBConnect connection, string sUniqueKeyHead850, Dictionary<string, string> sValsFrom860)
        {
            Dictionary<string, string> sRetVals = GetUpdate850Vals(sValsFrom860);

            sRetVals.Add(_Column.UniqueKey, sUniqueKeyHead850);
            sRetVals.Add(_Column.UniqueItem, connection.GetNewKey());

            return sRetVals;
        }

        private static Dictionary<string, string> GetUpdate850Vals(Dictionary<string, string> sValsFrom860)
        {
            Dictionary<string, string> sRetVals = new Dictionary<string, string>()
            {
                {_Column.TrxDate, sValsFrom860[_Column.TrxDate]},
                {_Column.PONumber, sValsFrom860[_Column.PONumber]},
                {_Column.POLine, sValsFrom860[_Column.POLine]},
                {_Column.MarkForID, sValsFrom860[_Column.MarkForID]},
                {_Column.Quantity, sValsFrom860[_Column.ChangeQuantity]},
                {_Column.UnitMeasure, sValsFrom860[_Column.UnitMeasure]},
                {_Column.PriceBasis, sValsFrom860[_Column.PriceBasis]},
                {_Column.GTIN, sValsFrom860[_Column.GTIN]},
                {_Column.UPCNum, sValsFrom860[_Column.UPCNum]},
                {_Column.VendorNum, sValsFrom860[_Column.VendorNum]},
                {_Column.BuyerNum, sValsFrom860[_Column.BuyerNum]},
                {_Column.ItemDesc, sValsFrom860[_Column.ItemDesc]},
                {_Column.ItemColor, sValsFrom860[_Column.ItemColor]},
                {_Column.ItemSize, sValsFrom860[_Column.ItemSize]},
                {_Column.InPackUm, sValsFrom860[_Column.InPackUm]},
                {_Column.Dropship, sValsFrom860[_Column.Dropship].Replace('\r',' ')},
                {_Column.RecvID, sValsFrom860[_Column.RecvID]},
                {_Column.SendID, sValsFrom860[_Column.SendID]},
                {_Column.DetailNotes, sValsFrom860[_Column.DetailNotes]},
                {_Column.Partner, sValsFrom860[_Column.Partner]}
            };

            decimal temp = 0;

            new List<string>()
            {
                _Column.UnitPrice,
                _Column.RetailPrice,
                _Column.PackSize,
                _Column.InPackSize
            }.ForEach(key =>
            {
                if (decimal.TryParse(sValsFrom860[key], out temp) && temp > 0)
                {
                    sRetVals.Add(key, sValsFrom860[key]);
                }
            });

            return sRetVals;
        }

        #endregion Change Type Handling

        #region Flag Updates

        private static void UpdateAll860ProcFlags(DBConnect connection, string sUniqueKey, string sFlag)
        {
            string filterUniqueKey = string.Format("WHERE {0}='{1}'", _Column.UniqueKey, sUniqueKey);

            var headUpdateVals = new Dictionary<string, string>()
            {
                {_Column.HProcessed, sFlag}
            };

            var detlUpdateVals = new Dictionary<string, string>()
            {
                {_Column.Processed, sFlag}
            };

            if (sFlag == _ProgressFlag.Success || sFlag == _ProgressFlag.Canceled)
            {
                headUpdateVals.Add(_Column.ProcessedDate, DateTime.Now.ToString("yyyy-MM-dd"));
            }

            connection.Update(_Table.Head860, headUpdateVals, filterUniqueKey);
            connection.Update(_Table.Detail860, detlUpdateVals, filterUniqueKey);
        }

        private static void UpdateHead860ProcFlag(DBConnect connection, string sUniqueKey, string sFlag)
        {
            var headUpdateVals = new Dictionary<string, string>()
            {
                {_Column.HProcessed, sFlag}
            };

            if (sFlag == _ProgressFlag.Success || sFlag == _ProgressFlag.Canceled)
            {
                headUpdateVals.Add(_Column.ProcessedDate, DateTime.Now.ToString("yyyy-MM-dd"));
            }

            connection.Update(_Table.Head860, headUpdateVals, string.Format("WHERE {0}='{1}'", _Column.UniqueKey, sUniqueKey));
        }

        private static void UpdateDetl860ProcFlag(DBConnect connection, string sUniqueKey, string sUniqueItem, string sFlag)
        {
            var detlUpdateVals = new Dictionary<string, string>()
            {
                {_Column.Processed, sFlag}
            }.ToNameValueCollection();

            connection.Update(_Table.Detail860, detlUpdateVals, string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.UniqueKey, sUniqueKey, _Column.UniqueItem, sUniqueItem));
        }

        #endregion Flag Updates
    }
}