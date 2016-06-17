/// <reference path="../jquery.js" />
function disableRow(index) {
    var cButtonTD = $("button.cancelButton[index='" + index + "']").parent();
    var aButtonTD = $("button.applyButton[index='" + index + "']").parent();
    var row = cButtonTD.parent();
    cButtonTD.remove();
    aButtonTD.remove();
    $(row).removeClass("valid");
    $(row).addClass("invalid");
    $(row).append($("<td>").text("Invalid"), $("<td>").text("Invalid"));
}

function applyTableStyling() {
    $(".dataTable > tbody > tr").removeClass("TblRowLight").removeClass("TblRowDark");
    var rows = $(".dataTable > tbody > tr");
    rows.filter(":odd").addClass("TblRowLight");
    rows.filter(":even").addClass("TblRowDark");
}

function applyTableHeaderStyle(div) {
    var TableHead = div.find('.TblHeader');
    var TableBody = TableHead.next().find('.PaddedTbl');
    var HeaderCols = TableHead.find('th');
    var DataCols = TableBody.find('tr:visible:first td');
    var Length = HeaderCols.length;
    for (var i = 0; i < Length; i++) {
        $(HeaderCols[i]).css('width', $(DataCols[i]).css('width'));
    }
    TableHead.find('tr').css('width', TableBody.find('tr:visible:first').css('width')).css('display', 'block');
}

(function () {
    $(function () {
        loadPOChanges();
        applyTableStyling();
    });

    function loadPOChanges() {
        if (CPODetails.length !== 0) {
            $("#noContentPara").hide();
            $("#contentDiv").append(createHeaderTable("Pending PO Changes", CPODetails));
            applyTableStyling();
            applyTableHeaderStyle($('#POchgHdr'));
        }
        else {
            $("#contentDiv").html('<p id="noContentPara">No PO changes present.</p>');
        }
    }

    function createHeaderTable(headerText, reportDetails) {
        var key = genKey();
        var divContent = $("<div id='POchgHdr'>");
        if (reportDetails.length === 0) {
            return divContent;
        }
        var trHead = $("<tr>").prop("align", "center").append(
            $("<th>").prop("scope", "col").text("Date"),
            $("<th>").prop("scope", "col").text("PO #"),
            $("<th>").prop("scope", "col").text("Purpose"),
            $("<th>").prop("scope", "col").text("Items").addClass("tooltip").attr("data-tooltip","Number of items affected"),
            $("<th>").prop("scope", "col").text("Detail").addClass("tooltip").attr("data-tooltip","View item details"),
            $("<th>").prop("scope", "col").text("Cancel"),
            $("<th>").prop("scope", "col").text("Apply")
        );

        var thead = $("<thead>").append(trHead);
        var tbody = $("<tbody>");

        for (var i = 0; i < reportDetails.length; i++) {
            var row = $("<tr>").attr("rowindex", i).append(
                $("<td>").text(reportDetails[i].POChangeDate),
                $("<td>").text(reportDetails[i].PONumber),
                $("<td>").text(reportDetails[i].Purpose),
                $("<td>").text(reportDetails[i].Affected),
                $("<td>").append($("<button>").addClass("button detailButton btnSm").prop("type", "button").attr("index", i).text("Detail"))
            );
            if ((reportDetails[i].Affected === "0" && reportDetails[i].Purpose === "Change") || reportDetails[i].Status === "X") {
                $(row).append(
                    $("<td>"),
                    $("<td>").text("Invalid")
                );
                $(row).addClass("invalid RemovedItem");
            }
            else {
                $(row).append(
                    $("<td>").append($("<button>").addClass("button cancelButton btnSm").prop("type", "button").attr("index", i).text("Cancel")),
                    $("<td>").append($("<button>").addClass("button applyButton btnSm").prop("type", "button").attr("index", i).text("Apply"))
                );
                $(row).addClass("valid");
            }
            tbody.append(row);
        }

        var headTable = $("<table class='TblHeader PaddedTbl'>").append(thead);
        var reportTable = $("<table>").prop({ "class": "dataTable prevTable blueBg PaddedTbl scrollTable", "cellspacing": "0", "rules": "all", "border": "1", "id": "detailTable" }).css("border-collapse", "collapse").append(
            tbody
        );

        $(".detailButton", reportTable).click(function () {
            var index = $(this).attr("index");
            var num = "PO #" + CPODetails[index].PONumber;
            var date = "[" + CPODetails[index].POChangeDate + "]";
            var purp = CPODetails[index].Purpose;
            var header = date + " " + num + " " + purp;

            var dWindow = new DisplayWindow({ Size: 'large' });

            dWindow.Content.html(
                $("<div>").prop("id", "reportDiv").addClass("BtmMargin").append(
                    $("<div>").prop("class", "RedBottomHead").append(
                        $("<h2>").prop("class", "tooltip-head").text(header),
                        $("<div>").css("clear", "both")
                    ),
                    createReportTable("", CPODetails[index].Details)
                )
            );

            $("<input type='button' class='button CloseWindowBtn' value='Close Window'>").appendTo(dWindow.Content);
            dWindow.show();
            ReposWindow(dWindow);
            applyTableStyling();
        });
        $(".cancelButton", reportTable).click(function () {
            var i = $(this).attr("index");
            doAction(i, CPODetails[i].UniqueKey, "Cancel");
        });
        $(".applyButton", reportTable).click(function () {
            var i = $(this).attr("index");
            doAction(i, CPODetails[i].UniqueKey, "Apply");
        });

        divContent.append(headTable);
        var divScroll = $("<div class='BtmMargin TblScroll TblScrollShort'>").append(reportTable);
        divContent.append(divScroll);
        return divContent;
    }

    function doAction(index, key, action) {
        var request = {
            Key: key,
            Action: action
        };
        var doneFunc = function (response) {
            Working.End();
            var respObj = JSON.parse(response.d);
            var dWindow = new DisplayWindow({ Size: 'small' });
            if (respObj.success)
            {
                dWindow.Content.append(
                    $("<div>").html(respObj.data),
                    $("<input type='button' class='button CloseWindowBtn' value='Close Window'>").css("margin-bottom", "10px")
                );
                $("tr[rowindex='" + index + "']").remove();
            }
            else
            {
                dWindow.Content.append(
                    $("<div>").html(respObj.data.msg),
                    $("<input type='button' class='button CloseWindowBtn' value='Close Window'>").css("margin-bottom", "10px")
                );
                disableRow(index);
            }
            applyTableStyling();
            dWindow.show();
        };

        var failFunc = function () {
            Working.End();
            var dWindow = new DisplayWindow({ Size: 'small' });
            dWindow.Content.append(
                $("<div>").html("An AJAX error occurred. Please contact EDI Options."),
                $("<input type='button' class='button CloseWindowBtn' value='Close Window'>")
            );
            dWindow.show();
        };

        Working.Begin();

        submitAJAXRequest("ChangePO.aspx/DoAction", request, doneFunc);
    }

    function createReportTable(headerText, reportDetails) {
        var key = genKey();
        if (reportDetails.length === 0) {
            return createCollapsibleSection(headerText, key, $("<table>"));
        }
        var trHead = $("<tr>").prop("align", "center").css({ "border-color": "transparent", "font-size": "14px" }).append(
            $("<th>").prop("scope", "col").text("Change Type"),
            $("<th>").prop("scope", "col").text("Old Quantity"),
            $("<th>").prop("scope", "col").text("New Quantity"),
            $("<th>").prop("scope", "col").text("Unit Price"),
            $("<th>").prop("scope", "col").text("Retail Price"),
            $("<th>").prop("scope", "col").text("UPC #"),
            $("<th>").prop("scope", "col").text("Vendor #"),
            $("<th>").prop("scope", "col").text("Description"),
            $("<th>").prop("scope", "col").text("Pack Size"),
            $("<th>").prop("scope", "col").text("Dropship")
        );

        var thead = $("<thead>").append(trHead);
        var tbody = $("<tbody>");

        for (var i = 0; i < reportDetails.length; i++) {
            var row = $("<tr>").css("border-color", "transparent").append(
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].ChangeType),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].Quantity),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].ChangeQuantity),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].UnitPrice),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].RetailPrc),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].UPC),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].VendorNum),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].ItemDesc),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].PackSize),
                $("<td>").prop({ "align": "left" }).text(reportDetails[i].Dropship)
            );
            tbody.append(row);
        }

        var reportTable = $("<table>").prop({ "class": "dataTable prevTable blueBg PaddedTbl scrollTable", "cellspacing": "0", "rules": "all", "border": "1", "id": "detailTable" }).css("border-collapse", "collapse").append(
                thead,
                tbody
            );

        return reportTable;
    }

    function createCollapsibleSection(headerText, key, content) {
        var handleContainer = $("<p>").addClass("expand-handle").attr("id", key + "_handle").click(function () {
            if ($(this).hasClass('expand-handle-open')) {
                $(this).next("div").slideUp();
                $(this).removeClass("expand-handle-open");
            }
            else {
                $(this).addClass("expand-handle-open");
                $(this).next("div").slideDown();
            }
        }).append($("<span>").html(headerText));

        var dataContainer = $("<div>").addClass("expand-content").attr("data-prtid", key).css({ "border-bottom": "0px" }).append(
                content
            );
        return $("<div>").append(
            handleContainer,
            dataContainer
        );
    }
} ());