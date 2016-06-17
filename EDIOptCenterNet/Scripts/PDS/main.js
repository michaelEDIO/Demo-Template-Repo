(function () {
    var poTable;
    var reportTable;
    var reportWindow;
    var submitPoListWindow;
    var summaryPoWindow;

    $(function () {
        Working.Begin();
        submitAJAXRequest("PoDropShip.aspx/InitPoList", "", function (response) {
            Working.End();
            var resp = JSON.parse(response.d);
            if (resp.success) {
                resp = resp.data;
                var mainTable = $("#mainDiv");
                poTable = new PageTable(CreateMainTableOptions(resp));
                poTable.SetFilter(resp.TableState.Filter);
                mainTable.empty();
                poTable.Create(mainTable);
            }
        });
    });

    function CreateMainTableOptions(resp) {
        return {
            title: "PO Drop Ship Manager",
            criteriaColumnCount: 1,
            isAdvancedSearch: true,
            isRefine: true,
            isCollection: true,
            columns: resp.TableState.Columns,
            rows: resp.TableData,
            replaces: {
                "invtotal": function (index, row) {
                    return $("<div>").addClass(row.invstatus).text(row.invtotal);
                },
                "asntotal": function (index, row) {
                    return $("<div>").addClass(row.asnstatus).text(row.asntotal);
                }
            },
            onFilterUpdate: function (filterInfo) {
                Working.Begin();
                submitAJAXRequest("PoDropShip.aspx/GetPoList", filterInfo, function (response) {
                    Working.End();
                    var resp = JSON.parse(response.d);
                    if (resp.success) {
                        resp = resp.data;
                        var mainTable = $("#mainDiv");
                        poTable.SetFilter(resp.TableState.Filter);
                        poTable.SetRows(resp.TableData);
                        poTable.SetColumns(resp.TableState.Columns);
                        mainTable.empty();
                        poTable.Create(mainTable);
                    }
                });
            },
            TableToolbarEditButtons: function () {
                return $("<div>").append(
                    $("<div>").addClass("downloadDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Download"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarDownloadBtn tooltip downloadInv").attr("data-tooltip", "Download").click(openReportList)
                        )
                    ),
                    $("<div>").addClass("submitDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Submit"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarAckBtn tooltip submitInv").attr("data-tooltip", "Submit").click(openSubmitPoList)
                        )
                    )
                );
            }
        };
    }

    function openReportList() {
        var submitted = poTable.GetCheckedRows();
        if (submitted.length == 0) {
            ShowMessage("Download Error", "No POs are selected. Select POs and try again.", false);
            return;
        }
        var keyList = [];

        for (var i = 0; i < submitted.length; i++) {
            keyList.push(submitted[i].key);
        }

        Working.Begin();
        submitAJAXRequest("PoDropShip.aspx/GetDownloadReports", keyList, function (response) {
            Working.End();
            var resp = JSON.parse(response.d);
            if (resp.success) {
                var dlList = resp.data;
                if (reportWindow) {
                    reportWindow.close();
                }
                reportWindow = new DisplayWindow({ Size: 'medium' });

                reportWindow.Content.html(createReportWindow(dlList));

                reportWindow.show();
                applyTableStyling();
                applyTableHeaderStyle($("#downloadRecDiv"));
            }
        });
    }

    function openSubmitPoList() {
        var input = "";
        var submitted = poTable.GetCheckedRows();

        for (var i = 0; i < submitted.length; i++) {
            input += submitted[i].ponumber + "\n";
        }

        if (submitPoListWindow) {
            submitPoListWindow.close();
        }
        submitPoListWindow = new DisplayWindow({ Size: 'small' });

        submitPoListWindow.Content.html($("<div>").append(
            $("<h2>").addClass("RedBottomHead").text("Submit Purchase Orders"),
            $("<p class='SmallGreyTxt'>").html("Select the POs you'd like to process. Please enter one PO # per line."),
            $("<div class='BtmMargin'>").append(
                $("<textarea>").attr("id", "poDiv").width("485").height("200").text(input)
            ),
            $("<div>").addClass("submitDiv").append(
                $("<button>").prop({ "type": "button", "id": "closeButton" }).addClass("button").html("SUBMIT").css("margin-right", "10px").click(function () {
                    var pack = $("#poDiv").val().trim();

                    if (pack.length <= 0) {
                        ShowMessage("Submit Error", "Enter the PO numbers to submit and try again.", false);
                        return false;
                    }
                    Working.Begin();
                    submitAJAXRequest("PoDropShip.aspx/VerifyPoList", pack, function (response) {
                        Working.End();
                        var resp = JSON.parse(response.d);
                        if (resp.success) {
                            var dlList = resp.data;

                            if (dlList.length <= 0) {
                                ShowMessage("Submit Error", "None of the sent POs could be found. Check your submission and try again.", false);
                                return false;
                            }

                            submitPoListWindow.close();

                            if (summaryPoWindow) {
                                summaryPoWindow.close();
                            }
                            summaryPoWindow = new DisplayWindow({ Size: 'medium' });

                            summaryPoWindow.Content.html(createSummaryPoWindow(dlList));

                            summaryPoWindow.show();
                            applyTableStyling();
                            applyTableHeaderStyle($("#summary"));
                        }
                    });
                    return false;
                }),
                $("<button>").prop({ "type": "button", "id": "closeButton" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
            )
        ));

        submitPoListWindow.show();

        $("#poDiv").focus();
    }

    function createReportWindow(reportList) {
        var columnHeaders = [
            { Header: "PO #", Column: "ponumber" },
            { Header: "Trx", Column: "trxtype" },
            { Header: "Description", Column: "description" },
            { Header: "Download", Column: "dl" }
        ];

        var bolHead = { "title": "Edit BOL", "type": "text", "placeholder": "BOL #" };
        var rtHead = { "title": "Edit Routing", "type": "text", "placeholder": "Routing" };
        var aplHead = { "id": "bulkApplyButton", "data-tooltip": "Apply Changes" };

        var reportTableOptions = {
            title: "Reports",
            isSearchEnabled: false,
            isPagination: false,
            isCriteriaEnabled: false,
            columns: columnHeaders,
            rows: reportList,
            replaces: {
                "dl": function (index, row) {
                    var ret = $("<td>").append(
                        $("<button>").prop({ "type": "button", "index": index }).addClass("button btnSm").html("DOWNLOAD").click(function () {
                            downloadReports([row.filepath], [row.key]);
                            return false;
                        })
                    );
                    return ret;
                }
            }
        };

        var reportTable = new PageTable(reportTableOptions);

        var table = $("<div>").attr("id", "downloadRecDiv");
        reportTable.Create(table);

        var container = $("<div>");

        var footer = $("<div>").addClass("submitDiv").append(
            $("<button>").prop({ "type": "button", "id": "closeButton" }).addClass("button").html("DOWNLOAD ALL").css("margin-right", "10px").click(function () {
                var rows = reportTable.GetRows();
                var keyList = [];
                var pathList = [];
                for (var i = 0; i < rows.length; i++)
                {
                    keyList.push(rows[i].key);
                    pathList.push(rows[i].filepath);
                }
                downloadReports(pathList, keyList);
                reportWindow.close();
            }),
            $("<button>").prop({ "type": "button", "id": "closeButton" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
        );

        container.append(table, footer);
        return container;
    }

    function createSummaryPoWindow(editList) {
        var columnHeaders = [
            { Header: "PO #", Column: "ponumber" },
            { Header: "Invoice #", Column: "invoiceno" },
            { Header: "BOL #", Column: "bolnumber" }
        ];

        var summaryTableOptions = {
            title: "Purchase Order Summary",
            isSearchEnabled: false,
            isPagination: false,
            isCriteriaEnabled: false,
            columns: columnHeaders,
            rows: editList,
            replaces: {
                "invoiceno": replaceInv,
                "bolnumber": replaceBOL
            }
        };

        var summaryTable = new PageTable(summaryTableOptions);

        var table = $("<div>").attr("id", "summary");
        summaryTable.Create(table);

        var container = $("<div>");

        var submitFunc = function () {
            var updatePack = [];
            var bolCol = $(".bolBox");
            var invCol = $(".invBox");
            
            for (var i = 0; i < bolCol.length; i++) {
                var bVal = $(bolCol[i]).val();
                var iVal = $(invCol[i]).val();
                var bIndex = $(bolCol[i]).attr("index");
                if (bVal !== "") {
                    var data = {
                        key: editList[bIndex].key,
                        ponumber: editList[bIndex].ponumber,
                        bolnumber: bVal,
                        invoiceno: iVal
                    };
                    updatePack.push(data);
                }
            }
            Working.Begin();
            submitAJAXRequest("PoDropShip.aspx/SubmitPoList", updatePack, function (response) {
                Working.End();
                summaryPoWindow.close(); 
                var resp = JSON.parse(response.d);
                postSendRecord(resp);
            });
            return false;
        }

        var footer = $("<div>").addClass("submitDiv").append(
            $("<button>").prop({ "type": "button" }).addClass("button").html("SUBMIT").css("margin-right", "10px").click(submitFunc),
            $("<button>").prop({ "type": "button", "id": "closeButton" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
        );

        container.append(table, footer);
        return container;
    }

    function postSendRecord(resp) {
        if (resp.success && resp.data.length > 0) {
            validatePL(resp.data);
        }
        else {
            poTable.TriggerUpdate();
            ShowMessage("Submit Error", "An unknown error occurred when attempting to submit. If the problem continues to persist, please contact EDI Options.", false);
        }
    }

    function validatePL(keyList) {
        Working.Begin();
        PostToOC("PLVALIDATE", { TrxList: keyList }, function (data) {
            Working.End();
            poTable.TriggerUpdate();
            if (data.error) {
                ShowMessage("ASN Validation Error", data.message, false);
            }
            else {
                var item, ul;
                var errCount = 0;
                var errDiv = $("<div>");
                var goodArr = [];
                for (var i = 0; i < data.pldata.length; i++) {
                    item = data.pldata[i];
                    if (item.error) {
                        errCount++;
                        errDiv.append($('<p>').addClass('SmallGreyTxt').html("BOL #: " + item.bol));
                        ul = $("<ul>");
                        for (var e = 0; e < item.msgs.length; e++) {
                            ul.append($('<li>').html(item.msgs[e]));
                        }
                        errDiv.append(ul);
                    }
                    else {
                        goodArr.push(item.bol);
                    }
                }

                var cWindow = new DisplayWindow({ Size: (errCount > 0 ? 'large' : 'small') });
                cWindow.Content.append('<div class="RedBottomHead"><h2>Submission Notice</h2></div>');
                if (errCount > 0) {
                    cWindow.Content.append($('<p>').addClass('caution SmBtmMargin').text('Warning!'));
                    cWindow.Content.append($('<p>').addClass('SmallGreyTxt').text("Your data did not pass validation for " + errCount + " of " + data.pldata.length + " " + "ASN. Please correct the errors below and try again."));
                }
                else {
                    cWindow.Content.append($('<p>').addClass('good SmBtmMargin').text('Success!'));
                    cWindow.Content.append($('<p>').addClass('SmallGreyTxt').css('margin', '0').text("Your data passed validation for " + data.pldata.length + " " + "ASNs !"));
                }
                cWindow.Content.append(errDiv);
                if (goodArr.length > 0) {
                    cWindow.Content.append($('<p>').addClass('SmallGreyTxt BtmMargin').html("The following BOL numbers were validated: " + goodArr.join(', ') + "."));
                }
                cWindow.Content.append('<input type="button" class="button display-CloseWindowBtn" value="Close Window">');
                cWindow.show();
            }
        });
    }

    function downloadReports(fileList, keyList) {
        PostToOC("DLREPORTS", { FileList: fileList, TrxKeys: keyList }, function (data) {
            if (data.error) {
                ShowMessage("Download Error", data.message, false);
            }
        });
    }

    function replaceBOL(index, row) {
        return $("<input>").addClass("bolBox").prop("type", "text").attr("index", index).val(row.bolnumber);
    }

    function replaceInv(index, row) {
        return $("<input>").addClass("invBox").prop("type", "text").attr("index", index).val(row.invoiceno);
    }
} ());