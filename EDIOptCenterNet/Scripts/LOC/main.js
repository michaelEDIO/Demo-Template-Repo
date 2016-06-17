(function () {
    var stList = [];
    var aeList = [];
    var bnList = [];
    var stDict = {};

    var STManager = _IdHolder();
    var BNManager = _IdHolder();
    var AEManager = _IdHolder();

    var startDate = $("#startDate");
    var endDate = $("#endDate");

    var noSTMessage = "No Stores Selected.";
    var noBNMessage = "No Brand Names Selected.";
    var noAEMessage = "No Account Executives Selected.";

    var isHideFilter = false;

    $(function () {
        startDate.datepicker({
            dateFormat: "M dd yy"
        });
        endDate.datepicker({
            dateFormat: "M dd yy"
        });

        createStaticAttribute($("<span>").append($("<span>").css("font-weight", "bold").text("Stores: "), $("<span>").attr("id", "stMessage").text(noSTMessage)), function () {
            STManager.Clear();
            updateSTMessage();
        });

        $("#addSTButton").click(function () {
            _createAttrModWindow("Add/Remove Stores", "Select stores to add to the filter.", STManager, stList.length, function (i) {
                return "st_" + i;
            }, function (i) {
                return stList[i].BYID;
            }, function (i) {
                return (stList[i].DisplayID) + " - " + stList[i].Name;
            }, updateSTMessage);
        });

        $("#reportButton").click(submitRequest);

        submitAJAXRequest("LocationReport.aspx/ShowFilter", "", function (response) {
            var resp = JSON.parse(response.d);
            if (resp.success) {
                locOptions = resp.data;

                $("#reportType").append(
                    locOptions.Option.Location ? $("<option>").attr({ "value": "Location", "selected": "selected" }).text("Location Report") : "",
                    locOptions.Option.Asn ? $("<option>").attr("value", "ASNSummary").text("ASN Summary Report") : ""
                );

                if (locOptions.Query.Bol) {
                    $("#bolFilter").show();
                }

                if (locOptions.Filter.Brand) {
                    $("#dateTable tbody").append(
                        $("<tr>").append(
                            $("<td>").append(
                                $("<span>").addClass("LeftLabelWide").text("Brand Names:")
                            ),
                            $("<td>").append(
                                $("<button>").addClass("button").attr({ "type": "button", "id": "addBNButton" }).text("Add").click(function () {
                                    _createAttrModWindow("Add/Remove Brand Names", "Select brand names to add to the filter.", BNManager, bnList.length, function (i) {
                                        return "bn_" + i;
                                    }, function (i) {
                                        return bnList[i];
                                    }, function (i) {
                                        return bnList[i];
                                    }, updateBNMessage);
                                })
                            )
                        )
                    );

                    createStaticAttribute($("<span>").append($("<span>").css("font-weight", "bold").text("Brand Names: "), $("<span>").attr("id", "bnMessage").text(noBNMessage)), function () {
                        BNManager.Clear();
                        updateBNMessage();
                    });
                }

                if (locOptions.Filter.AE) {
                    $("#dateTable tbody").append(
                        $("<tr>").append(
                            $("<td>").append(
                                $("<span>").addClass("LeftLabelWide").text("Account Executives:")
                            ),
                            $("<td>").append(
                                $("<button>").addClass("button").attr({ "type": "button", "id": "addAEButton" }).text("Add").click(function () {
                                    _createAttrModWindow("Add/Remove Account Executives", "Select account executives to add to the filter.", AEManager, aeList.length, function (i) {
                                        return "ae_" + i;
                                    }, function (i) {
                                        return aeList[i];
                                    }, function (i) {
                                        return aeList[i];
                                    }, updateAEMessage);
                                })
                            )
                        )
                    );

                    createStaticAttribute($("<span>").append($("<span>").css("font-weight", "bold").text("Account Executives: "), $("<span>").attr("id", "aeMessage").text(noAEMessage)), function () {
                        AEManager.Clear();
                        updateAEMessage();
                    });
                }

                Working.Begin();
                submitAJAXRequest("LocationReport.aspx/GetFilters", "", function (response) {
                    var resp = JSON.parse(response.d);
                    if (resp.success) {
                        stList = resp.data.STList;
                        aeList = resp.data.AEList;
                        bnList = resp.data.BNList;
                        for (var i = 0; i < stList.length; i++) {
                            stDict[stList[i].BYID] = stList[i].DisplayID;
                        }
                    }
                    Working.End();
                });
            }
        });
    });

    function updateSTMessage() {
        var ids = STManager.GetList().map(function (elem) {
            return stDict[elem];
        }).join(", ");

        if (ids.length > 0) {
            $("#stMessage").text(ids);
        }
        else {
            $("#stMessage").text(noSTMessage);
        }
    }

    function updateBNMessage() {
        _updateMsg(BNManager, "#bnMessage", noBNMessage);
    }

    function updateAEMessage() {
        _updateMsg(AEManager, "#aeMessage", noAEMessage);
    }

    function submitRequest() {
        var blQuery = $("#bolBox").val();
        var poQuery = $("#poBox").val();
        var tp = $("#reportType").val();
        var sd = new Date(startDate.val());
        var ed = new Date(endDate.val());
        if (sd == "Invalid Date") {
            $("#startDateInvalid").show();
        }
        else {
            $("#startDateInvalid").hide();
        }
        if (ed == "Invalid Date") {
            $("#endDateInvalid").show();
        }
        else {
            $("#endDateInvalid").hide();
        }
        if (sd != "Invalid Date" && ed != "Invalid Date") {
            var request = {
                StartDate: sd,
                EndDate: ed,
                Type: tp,
                Stores: STManager.GetList(),
                BrandNames: BNManager.GetList(),
                AENames: AEManager.GetList(),
                BOLQuery: blQuery,
                POQuery: poQuery
            };
            Working.Begin();
            var goodFunc = function (response) {
                Working.End();
                var resp = JSON.parse(response.d);
                if (resp.success) {
                    var ts = new Date();
                    var timestamp = pad4(ts.getFullYear()) + pad2(ts.getMonth() + 1) + pad2(ts.getDate()) + pad2(ts.getHours()) + pad2(ts.getMinutes()) + pad2(ts.getSeconds()) + pad3(ts.getMilliseconds());
                    window.location.replace("download/report-" + timestamp + ".xlsx/" + resp.data);
                }
                else {
                    ShowSmallAlertWindow("Unable to Create Report", resp.type, resp.data.msg);
                }
                Working.End();
            };
            var failFunc = function () {
                Working.End();
            };
            submitAJAXRequest("LocationReport.aspx/GenerateReport", request, goodFunc, failFunc);
        }
    }

    function pad4(num) {
        return ("000" + num).slice(-4);
    }

    function pad3(num) {
        return ("00" + num).slice(-3);
    }

    function pad2(num) {
        return ("0" + num).slice(-2);
    }

    function stripeAttributes() {
        $(".StripedDiv:visible:odd").css("background-color", "#252525");
        $(".StripedDiv:visible:even").css("background-color", "");
    }

    function createStaticAttribute(content, onClickMinus) {
        _createAttrDiv("#FilterCriteriaDiv", content, onClickMinus, genKey());
    }

    function _createAttrDiv(selector, content, onClickMinus, key) {
        $(selector).append(
            $("<div>").prop({ "class": "RepeatableDiv StripedDiv", "id": key }).append(
                $("<div>").append(
                    $("<ul>").prop({ "class": "RemFilterBtnContainer AddSub", "title": "Click to remove filter" }).append(
                        $("<li>").prop("class", "RemFilterBtn").text("−").click(onClickMinus)
                    )
                ).append(content)
            )
        );
        stripeAttributes();
    }

    function _updateMsg(idHolder, divID, noIDMsg) {
        var ids = idHolder.GetList().join(", ");
        if (ids.length > 0) {
            $(divID).text(ids);
        }
        else {
            $(divID).text(noIDMsg);
        }
    }

    function _createAttrModWindow(header, subtext, idHolder, idCount, idFunc, valFunc, dispFunc, updateMsgFunc) {
        var selList = idHolder.GetList();

        var cList = $("<div>").addClass("checkList");

        for (var i = 0; i < idCount; i++) {
            var attrId = idFunc(i);
            var attrVal = valFunc(i);
            var attrDisp = dispFunc(i);
            var attrInput = {
                type: "checkbox",
                id: attrId,
                value: attrVal
            };
            if (selList.indexOf(attrVal) > -1) {
                attrInput["checked"] = "checked"
            }
            var elem = $("<div>").append(
                    $("<input>").attr(attrInput).click(function (event) {
                        if (this.checked) {
                            idHolder.Add($(this).val());
                        }
                        else {
                            idHolder.Remove($(this).val());
                        }
                        updateMsgFunc();
                    }),
                    $("<label>").attr({ "for": attrId }).text(attrDisp)
                );
            cList.append(elem);
        }

        var dWindow = new DisplayWindow({ Size: "small" });
        dWindow.Content.append(
                $("<div>").addClass("RedBottomHead").append(
                    $("<h2>").text(header)
                ),
                $("<p>").addClass("SmallGreyTxt").text(subtext),
                $("<div>").append(
                    $("<table>").addClass("PaddedTbl VerticalForm").append(
                        $("<tbody>").append(
                            $("<tr>").append(
                                $("<td>").append(
                                    cList
                                )
                            )
                        )
                    )
                ),
                $("<input>").attr({ "type": "button", "class": "button CloseWindowBtn", "value": "Close Window" })
            );

        dWindow.show();
    }

    function _IdHolder() {
        var cache = {
        },
        ct = 0;

        return {
            Add: function (id) {
                if (!cache.hasOwnProperty(id)) {
                    cache[id] = 1;
                    ct++;
                    return true;
                }
                return false;
            },
            Remove: function (id) {
                if (cache.hasOwnProperty(id)) {
                    delete cache[id];
                    ct--;
                    return true;
                }
                return false;
            },
            Clear: function () {
                cache = {
                };
                ct = 0;
            },
            GetList: function () {
                var idList = [];
                for (var id in cache) {
                    if (cache.hasOwnProperty(id)) {
                        idList.push(id);
                    }
                }
                return idList;
            },
            Count: function () {
                return ct;
            }
        };
    }
} ());
