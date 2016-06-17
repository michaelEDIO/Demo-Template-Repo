var WeekManager = (function () {
    var selectedWeeks = {},
        ct = 0;

    return {
        AddWeek: function (retailWeek) {
            if (this.CanAdd()) {
                var key = retailWeek.Week + "-" + retailWeek.Year;
                if (!selectedWeeks.hasOwnProperty(key)) {
                    selectedWeeks[key] = retailWeek;
                    ct = ct + 1;
                    return true;
                }
                return false;
            }
            return false;
        },
        RemoveWeek: function (retailWeek) {
            var key = retailWeek.Week + "-" + retailWeek.Year;
            if (selectedWeeks.hasOwnProperty(key)) {
                delete selectedWeeks[key];
                ct = ct - 1;
                return true;
            }
            return false;
        },
        CanAdd: function () {
            return ct < 7;
        },
        GetWeeks: function () {
            if (ct === 0) {
                // Take first 3 from existing list.
                return retailWeekList.slice(0, 3);
            }
            var ret = [];
            for (var wk in selectedWeeks) {
                if (selectedWeeks.hasOwnProperty(wk)) {
                    ret.push(selectedWeeks[wk]);
                }
            }
            return ret;
        },
        Count: function () {
            return ct;
        }
    };
} ());

var StoreManager = (function () {
    var storeCache = {},
        ct = 0;

    return {
        AddStore: function (store) {
            if (!storeCache.hasOwnProperty(store)) {
                storeCache[store] = 1;
                ct++;
                return true;
            }
            return false;
        },
        RemoveStore: function (store) {
            if (storeCache.hasOwnProperty(store)) {
                delete storeCache[store];
                ct--;
                return true;
            }
            return false;
        },
        ClearStores: function () {
            storeCache = {};
            ct = 0;
        },
        GetStoreList: function () {
            var storeList = [];
            for (var store in storeCache) {
                if (storeCache.hasOwnProperty(store)) {
                    storeList.push(store);
                }
            }
            return storeList;
        },
        Count: function () {
            return ct;
        }
    };
} ());

(function () {
    var allWeekKey = "allWeek";
    var allStoreKey = "allStore";

    $(function () {
        applyTableStylingStatus();
        applyListeners();

        if ($(".dataTable").length !== 0)
        {
            $("#emptyMessageSpan").hide();
        }

        // Default attributes: all stores, last 3 weeks
        createStaticStoreAttributeDiv($("<span><span style='font-weight:bold;'>Store: </span>No Stores Selected</span>"), allStoreKey);
        createStaticWeekAttributeDiv($("<span><span style='font-weight:bold;'>Time Frame: </span>Last 3 Retail Weeks</span>"), allWeekKey);
    });

    function applyListeners() {
        (function(){
            var monList = {
                "01":"Jan",
                "02":"Feb",
                "03":"Mar",
                "04":"Apr",
                "05":"May",
                "06":"Jun",
                "07":"Jul",
                "08":"Aug",
                "09":"Sep",
                "10":"Oct",
                "11":"Nov",
                "12":"Dec",
            };
            // Organize into years...
            var yearGroups = {};
            for (var i = 0; i < retailWeekList.length; i++){
                if (!(retailWeekList[i].Year in yearGroups))
                {
                    yearGroups[retailWeekList[i].Year] = [];
                }
                yearGroups[retailWeekList[i].Year].push(retailWeekList[i]);
            }
            var yearKeys = Object.keys(yearGroups);
            yearKeys.sort().reverse();
            for (var yr = 0; yr < Object.keys(yearGroups).length; yr++){
                var curYear = yearKeys[yr];
                var elemGroup = $("<optgroup>").prop({"label": "Retail Year " + curYear, "data-year": curYear});
                for (var wk = 0; wk < yearGroups[curYear].length; wk++)
                {
                    // 2013-01-01
                    // 5-7
                    // 9-10
                    var from = monList[yearGroups[curYear][wk].WeekStart.substring(5, 7)] + " " + yearGroups[curYear][wk].WeekStart.substring(8, 10);
                    var to   = monList[yearGroups[curYear][wk].WeekEnd.substring(5, 7)] + " " + yearGroups[curYear][wk].WeekEnd.substring(8, 10);

                    var weekTxt = "#" + yearGroups[curYear][wk].Week + ": " + from + " - " + to;

                    $("<option />", { value: JSON.stringify(yearGroups[curYear][wk]), text: weekTxt }).appendTo(elemGroup);
                }
                elemGroup.appendTo($("#weekSelect"));
            }
        }());

        $("#storeSelect").autocomplete({
            source: storeList,
            minLength: 3
        });

        $("#selectStoreButton").click(function () {
            var store = $("#storeSelect").val().trim();
            if(store.length > 0)
            {
                if (StoreManager.AddStore(store)) {
                    $("#" + allStoreKey).hide();
                    createStoreAttributeDiv($("<span><span style='font-weight:bold;'>Store: </span>" + store + "</span>"), function () {
                        StoreManager.RemoveStore(store);
                        if (StoreManager.Count() === 0) {
                            $("#" + allStoreKey).show();
                        }
                    });
                    $("#storeSelect").val("");
                }
            }
        });

        $("#selectWeekButton").click(function () {
            var retailWeek = JSON.parse($("#weekSelect").val());
            if (WeekManager.AddWeek(retailWeek)) {
                $("#" + allWeekKey).hide();
                createWeekAttributeDiv($("<span><span style='font-weight:bold;'>Time Frame: </span>Retail Week " + retailWeek.Week + ", " + retailWeek.Year + "</span>"), function () {
                    WeekManager.RemoveWeek(retailWeek);
                    if (WeekManager.Count() === 0) {
                        $("#" + allWeekKey).show();
                    }
                });
            }
        });

        $("#reportSelect").on("change",function(){
            var rpt = $("#reportSelect").val();
            if(rpt === "EST") { $("#submitRequestButton").hide(); }
            else {$("#submitRequestButton").show(); }
        });

        $("#submitRequestButton").click(function () {
            var check = $("#sendEmailCheckBox").prop("checked");
            var request = {
                Stores: StoreManager.GetStoreList(),
                Weeks: WeekManager.GetWeeks(),
                Email: check ? $("#email").val() : "",
                RequestType: $("#reportSelect").val()
            };

            var onSuccess = function (response) {
                var resp = JSON.parse(response.d);
                if (!resp.success){
                    ShowMessage("Request Submit Error", resp.data.msg, false);
                }
                var pastOrders = resp.data;

                // pastOrders contains new table data. Remove existing rows, make new ones.
                var tbody = $(".dataTable > tbody");
                if (tbody.length === 0) {
                    var reqDate = $("<th>").prop("scope", "col").css("width", "100px").text("Request Date");
                    var outFile = $("<th>").prop("scope", "col").css("width", "100px").text("Output File");
                    var email = $("<th>").prop("scope", "col").css("width", "100px").text("Email");
                    var status = $("<th>").prop("scope", "col").css("width", "100px").text("Status");

                    var tr = $("<tr>").prop("align", "center").css("border-color", "transparent").append(reqDate).append(outFile).append(email).append(status);

                    var head = $("<thead>").append(tr);
                    var body = $("<tbody>");

                    var table = $(document.createElement("table")).prop({ "class": "dataTable prevTable blueBg PaddedTbl", "cellspacing": "0", "rules": "all", "data-rel-tbl": "MainContent_pastOrderGrid", "border": "1", "id": "MainContent_pastOrderGrid" }).css("border-collapse", "collapse")
                    .append(head)
                    .append(body);

                    $("#pastRequestDiv > div").append(table);
                    tbody = body;
                }
                else {
                    tbody = tbody.empty();
                }

                for (var i = 0; i < pastOrders.length; i++) {
                    tbody.append(
                        $("<tr>").css("border-color", "transparent")
                        .append($(document.createElement("td")).prop("align", "left").text(pastOrders[i].RequestDate))
                        .append($(document.createElement("td")).prop("align", "left").text(pastOrders[i].OutputName))
                        .append($(document.createElement("td")).prop("align", "left").text(pastOrders[i].Email))
                        .append($(document.createElement("td")).prop({ "align": "left", "class": "status" }).text(pastOrders[i].Status))
                    );
                    $("#emptyMessageSpan").hide();
                }
                applyTableStylingStatus();
                Working.Hide();
            };
            Working.Show();
            
            submitAJAXRequest("SalesRequest.aspx/SendRequest", request, onSuccess);
        });

        $("#viewReportButton").click(function () {
            var check = $("#sendEmailCheckBox").prop("checked");
            var repType = $("#reportSelect").val();
            var request = {
                Stores: StoreManager.GetStoreList(),
                Weeks: WeekManager.GetWeeks(),
                Email: check ? $("#email").val() : "",
                RequestType: repType
            };

            var onSuccess = function (response) {
                var resp = JSON.parse(response.d);
                if (!resp.success){
                    ShowMessage("Report Error", resp.data.msg, false);
                }
                var reportDetails = resp.data;

                var dWindow = new DisplayWindow({Size: 'large'});

                var reportContainer = $("<div>");

                var sentStoreList = StoreManager.GetStoreList();

                for (var i = 0; i < reportDetails.length; i++){
                    reportContainer.append(
                        repType == "ESS" ? 
                                    createReportTable(sentStoreList.length === 0 ? "All Stores" : "Store #" + sentStoreList[i], reportDetails[i]) :
                                    createShipReportTable(sentStoreList.length === 0 ? "All Stores" : "Store #" + sentStoreList[i], reportDetails[i])
                    );
                }
                
                dWindow.Content.html( 
                    $("<div>").prop("id", "reportDiv").addClass("BtmMargin").append(
                        $("<div>").prop("class", "RedBottomHead").append(
                            $("<h2>").prop("class","tooltip-head").text("Report Details"),
                            $("<div>").css("clear","both")
                        ),
                        reportContainer
                    )
                );
                
                $("<input type='button' class='button CloseWindowBtn' value='Close Window'>").appendTo(dWindow.Content);
                dWindow.show();
                $(".WindowLg").css("top","60px");
                applyTableStylingStatus();
                $(".scrollTable").stickyTableHeaders();
                Working.Hide();
                $("#reportDiv .expand-handle:eq(0)").click(); //Expand 1st store
            };
            Working.Show();

            submitAJAXRequest("SalesRequest.aspx/GenerateReport", request, onSuccess);
        });
    }

    function createShipReportTable(headerText, reportDetails){
        // Each record is...
        var storeContainer = $("<div>");
        var storeHeader = headerText;
        var colNames = [
            "Vendor #",
            "UPC #",
            "Quantity"
        ];
        for(var i = 0; i < reportDetails.Shipments.length; i++) {
            // Append details of each shipment to main div.
            var tnTxt = reportDetails.Shipments[i].TrackingNumber;
            if (!tnTxt) {
                tnTxt = "[Blank]";
            }
            var qtyPl = reportDetails.Shipments[i].Quantity == 1 ? "" : "s";

            var headerShipment = "[" + reportDetails.Shipments[i].ShipDate + "] Shipment #" + tnTxt +", " + reportDetails.Shipments[i].Quantity + " piece" + qtyPl;
            var detailShipment = createDataTable("shipReportDiv" + i, 3, reportDetails.Shipments[i].Items.length, function(index){
                return $("<th>").prop("scope", "col").text(colNames[index]);
            }, function(index) {
                return $("<tr>").attr("rowindex", index).append(
                    $("<td>").text(reportDetails.Shipments[i].Items[index].VendorNum),
                    $("<td>").text(reportDetails.Shipments[i].Items[index].UPCNum),
                    $("<td>").text(reportDetails.Shipments[i].Items[index].Quantity)
                );
            });
            
            storeContainer.append(createCollapsibleSection(headerShipment, genKey(), detailShipment, function(dv){
                if (!dv.attr("width-set")) {
                    applyTableHeaderStyle(dv);
                    dv.attr("width-set", true);
                }
                
            }));
        }
        return createCollapsibleSection(storeHeader, genKey(), storeContainer);
    }

    function createReportTable(headerText, reportDetails) {
        var key = genKey();
        if (reportDetails.length === 0) {
            return createCollapsibleSection(headerText, key, $("<table>"));
        }
        var wItem = "50px";
        var wUnitCost = "80px";
        var wUnitPrice = "80px";
        var wOnHand = "50px";
        var wSold = "50px";
        var wOrder = "50px";
        var trHead = $("<tr>").prop("align","center").css({"border-color": "transparent", "font-size": "14pt"}).append(
            $("<th>").prop({"scope":"col", "colspan":"4","text-align":"center"}).text("Details")
        );
        var trDetail = $("<tr>").prop("align", "center").css({"border-color": "transparent", "font-size": "14px"}).append(
            $("<th>").prop("scope", "col").css("width", wItem).text("Item"), 
            $("<th>").prop("scope", "col").css("width", wUnitCost).text("Unit Cost"), 
            $("<th>").prop("scope", "col").css("width", wUnitPrice).text("Unit Price"),
            $("<th>").prop("scope", "col").css("width", wOnHand).text("On Hand")
        );

        for (var i = 0; i < reportDetails[0].Details.length; i++)
        {
            trHead.append(
                $("<th>").prop({"scope": "col","colspan": "2"}).text(
                    reportDetails[0].Details[i].RetailWeek.Week + "-" + reportDetails[0].Details[i].RetailWeek.Year
                )
            );
            trDetail.append(
                $("<th>").prop("scope", "col").css("width",wSold).text("S"),
                $("<th>").prop("scope", "col").css("width",wOrder).text("O")
            );
        }
        var thead = $("<thead>").append(trHead, trDetail);
        var tbody = $("<tbody>");

        var sumData = [];
        var sumOnHand = 0;
        for (var i = 0; i < reportDetails[0].Details.length; i++)
        {
            var sd = {};
            sd["s" + i] = 0;
            sd["o" + i] = 0;
            sumData.push(sd);
        }
        for (var i = 0; i < reportDetails.length; i++)
        {
            var row = $("<tr>").css("border-color","transparent").append(
                $("<td>").prop({"align": "left", "width": wItem}).text(reportDetails[i].VendorNum),
                $("<td>").prop({"align": "left", "width": wUnitCost}).text(reportDetails[i].UnitCost),
                $("<td>").prop({"align": "left", "width": wUnitPrice}).text(reportDetails[i].UnitPrice),
                $("<td>").prop({"align": "left", "width": wOnHand}).text(reportDetails[i].OnHand == '0' ? '-' : reportDetails[i].OnHand)
            );
            sumOnHand += reportDetails[i].OnHand;
            for (var j = 0; j < reportDetails[i].Details.length; j++)
            {
                var sold = reportDetails[i].Details[j].Sold;
                var order = reportDetails[i].Details[j].Order;
                sumData[j]["s" + j] += sold;
                sumData[j]["o" + j] += order;
                if (sold == '0') sold = '-';
                if (order == '0') order = '-';
                row.append($("<td>").prop({"align": "left", "width":wSold}).text(sold),
                            $("<td>").prop({"align": "left", "width":wOrder}).text(order));
            }
            tbody.append(row);
        }

        var sumRow = $("<tr>").css("border-color","transparent").append(
            $("<td>").prop({"align": "left", "width": wItem}).text("Total"),
            $("<td>").prop({"align": "left", "width": wUnitCost}).text("-"),
            $("<td>").prop({"align": "left", "width": wUnitPrice}).text("-"),
            $("<td>").prop({"align": "left", "width": wOnHand}).text(sumOnHand === 0 ?  "-" : sumOnHand)
        );
        for (var i = 0; i < sumData.length; i++)
        {
            sumRow.append($("<td>").prop({"align": "left", "width":wSold}).text(sumData[i]["s" + i] === 0 ? '-' : sumData[i]["s" + i]),
                          $("<td>").prop({"align": "left", "width":wOrder}).text(sumData[i]["o" + i] === 0 ? '-' : sumData[i]["o" + i]));
        }
        tbody.append(sumRow);
        
        var reportTable = $("<table>").prop({ "class": "dataTable prevTable blueBg PaddedTbl scrollTable", "cellspacing": "0", "rules": "all", "border": "1", "id": "detailTable" }).css("border-collapse", "collapse").append(
            thead,
            tbody
        );

        return createCollapsibleSection(headerText, key, reportTable);
    }

    function applyTableStylingStatus() {
        applyTableStyling();
        $(".status").each(function () {
            var type = $(this).text() === "Unprocessed" ? "neutral" : "good";
            if ($(this).find(".neutral,.good").length === 0)
            {
                $(this).contents().wrap("<span class='" + type + "'></span>");
            }
        });
    }

    function stripeAttributes() {
        $(".StripedDiv:visible:odd").css("background-color", "#252525");
        $(".StripedDiv:visible:even").css("background-color", "");
    }

    function createStaticStoreAttributeDiv(content, key) {
        _createStaticAttrDiv(".StoreCriteriaDiv", content, function () {}, key);
    }

    function createStaticWeekAttributeDiv(content, key) {
        _createStaticAttrDiv(".WeekCriteriaDiv",content, function(){},key);
    }

    function createStoreAttributeDiv(content, onRemove, key) {
        var usedKey = key || genKey();
        _createAttrDiv(".StoreCriteriaDiv", content, function () {
            onRemove();
            $("#" + usedKey).remove();
        }, usedKey);
    }

    function createWeekAttributeDiv(content, onRemove, key) {
        var usedKey = key || genKey();
        _createAttrDiv(".WeekCriteriaDiv", content, function () {
            onRemove();
            $("#" + usedKey).remove();
            stripeAttributes();
        }, usedKey);
    }

    function _createStaticAttrDiv(selector, content, onRemove, key){
        _createAttrDiv(selector, content, function () {
            onRemove();
        }, key || genKey());
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
} ());