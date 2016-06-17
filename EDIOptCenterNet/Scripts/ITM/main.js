(function () {
    var PackTypeDefault = "X";
    var PackTypePrepacked = "P";
    var PackTypeMixed = "M";
    var PackTypeDistributed = "D";

    var CartTypeManual = "X";
    var CartTypeAutomatic = "A";
    var CartTypePerASN = "S";
    var CartTypePerPO = "I";
    var CartTypePerPOStore = "B";

    var InvTypeDistributed = "D";
    var InvTypePrePacked = "P";
    var InvTypeMixed = "M";
    var InvTypeConsolidated = "C";

    var XTypeKeep = "X";
    var XTypeSplit = "S";
    var XTypeMerge = "M";

    var editWindow;
    var submitWindow;
    var recordTable;
    var options = {
        Date: "",
        Invoice: {
            Enabled: false
        },
        Packing: {
            Enabled: false,
            IsPP: false,
            IsMX: false,
            IsDS: false,
            Default: "1"
        },
        Shipping: {
            Carriers: []
        }
    };

    $(function () {
        Working.Begin();
        submitAJAXRequest("IntegrationManager.aspx/InitPendingList", "", function (response) {
            Working.End();
            var resp = JSON.parse(response.d);
            if (resp.success) {
                resp = resp.data;
                var mainTable = $("#pendingRecordDiv");
                options = resp.Options;
                recordTable = new PageTable(CreateMainTableOptions(resp));
                recordTable.SetFilter(resp.TableState.Filter);
                mainTable.empty();
                recordTable.Create(mainTable);
            }
        });
    });

    function isNone() {
        return !options.Invoice.Enabled && !options.Packing.Enabled;
    }

    function isInvOnly() {
        return options.Invoice.Enabled && !options.Packing.Enabled;
    }

    function isAsnOnly() {
        return !options.Invoice.Enabled && options.Packing.Enabled;
    }

    function isBoth() {
        return options.Invoice.Enabled && options.Packing.Enabled;
    }

    function CreateMainTableOptions(resp) {
        return {
            tableID: "pastRecords",
            title: "Integration Manager",
            isAdvancedSearch: true,
            criteriaColumnCount: 2,
            isRefine: true,
            isCollection: true,
            columns: resp.TableState.Columns,
            rows: resp.TableData,
            replaces: {
                "hprocessed": replaceProcFlag,
                "msg": replaceMsg
            },
            onFilterUpdate: function (filterInfo) {
                Working.Begin();
                submitAJAXRequest("IntegrationManager.aspx/GetPendingList", filterInfo, function (response) {
                    Working.End();
                    var resp = JSON.parse(response.d);
                    if (resp.success) {
                        resp = resp.data;
                        var mainTable = $("#pendingRecordDiv");
                        recordTable.SetFilter(resp.TableState.Filter);
                        recordTable.SetRows(resp.TableData);
                        recordTable.SetColumns(resp.TableState.Columns);
                        mainTable.empty();
                        recordTable.Create(mainTable);
                    }
                });
            },
            TableToolbarEditButtons: function () {
                return $("<div>").append(
                    $("<div>").addClass("editDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Edit"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarEditHeadBtn tooltip editInv").attr("data-tooltip", "Edit Invoices").click(chooseEdit)
                        )
                    ),
                    $("<div>").addClass("submitDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Submit"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarAckBtn tooltip submitInv").attr("data-tooltip", "Submit Invoices").click(chooseSubmit)
                        )
                    ),
                    $("<div>").addClass("alterDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Delete"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarResetBtn tooltip resetInv").attr("data-tooltip", "Reset Invoices").click(openReset),
                            $("<li>").addClass("ToolbarDeleteBtn tooltip removeInv").attr("data-tooltip", "Delete Invoices").click(openDelete)
                        )
                    )
                );
            }
        };
    }

    function createSendRequestKey(isSubAll, submitType, uniqueKeyList, sType, bInvNum) {
        return {
            isAll: isSubAll,
            subType: submitType,
            keyList: uniqueKeyList,
            filter: {},
            splitType: sType,
            baseInv: bInvNum
        };
    }

    function createSendRequestFilter(isSubAll, submitType, tableFilter, sType, bInvNum) {
        return {
            isAll: isSubAll,
            subType: submitType,
            keyList: [],
            filter: tableFilter,
            splitType: sType,
            baseInv: bInvNum
        };
    }

    function closeOpenWindows() {
        if (editWindow) {
            editWindow.close();
        }
        if (submitWindow) {
            submitWindow.close();
        }
    }

    function chooseEdit() {
        closeOpenWindows();
        var isSelected = recordTable.GetCheckedRowCount();
        if (isSelected > 0) {
            openEdit();
        }
        else {
            openEditAll();
        }
    }

    function chooseSubmit() {
        closeOpenWindows();
        var isSelected = recordTable.GetCheckedRowCount();
        if (isSelected > 0) {
            openSubmit();
        }
        else {
            openSubmitAll();
        }
    }

    function openEdit() {
        closeOpenWindows();
        var submitted = recordTable.GetCheckedRows(function (r) {
            return r["hprocessed"] === "Y";
        });

        if (submitted.length > 0) {
            ShowMessage("Edit Error", "You cannot edit a submitted invoice. Unselect processed invoices and try again.", false);
            return;
        }

        var editList = recordTable.GetCheckedRows(function (r) {
            return r["hprocessed"] !== "Y";
        });

        if (editList.length > 0) {
            editWindow = new DisplayWindow({
                Size: 'large'
            });
            editWindow.Content.html(createEditWindow(editList));
            editWindow.show();
            applyTableStyling();
            applyTableHeaderStyle($("#editRecords"));
        }
        else {
            ShowMessage("Edit Error", "Unable to edit selected invoices. Select invoices, and try again.", false);
        }
    }

    function openEditAll() {
        closeOpenWindows();
        Working.Begin();
        submitAJAXRequest("IntegrationManager.aspx/GetInvoiceCount", recordTable.GetFilter(), function (response) {
            Working.End();
            var resp = JSON.parse(response.d);
            if (resp.success) {
                if (resp.data == 0) {
                    ShowMessage("Edit Error", "There are no records you can bulk edit.", false);
                    return;
                }
                recCt = resp.data;
                var plural = recCt === 1 ? "" : "s";
                var filter = recordTable.GetFilter();
                var rows = recordTable.GetRows();

                editWindow = new DisplayWindow({
                    Size: 'large'
                });

                var bolSupl = $("<div>").addClass("SmBtmMargin").append(
                    $("<label for='editBolBox'>").css("margin-right", "10px").html("BOL"),
                    $("<input>").addClass("bolBox applyBox").attr("id", "editBolBox").prop("type", "text")
                );

                editWindow.Content.append(
                    $("<h2>").addClass("RedBottomHead").html("Edit Matched Invoices"),
                    $("<div>").addClass("SmBtmMargin").html("Editing the details of " + recCt + " invoice" + plural + ""),
                    $("<div>").addClass("SmBtmMargin").html("<br>"),
                    $("<div>").addClass("SmBtmMargin").append(
                        $("<label for='editBolBox'>").css("margin-right", "10px").html("BOL"),
                        $("<input>").addClass("bolBox applyBox").attr("id", "editBolBox").prop("type", "text")
                    ),
                    $("<div>").addClass("SmBtmMargin").append(
                        $("<label for='editShipDateBox'>").css("margin-right", "10px").html("Ship Date"),
                        $('<input type="text">').addClass("dateBox").attr("id", "editShipDateBox").datepicker({
                            dateFormat: 'M dd yy'
                        })
                    ),
                    $("<div>").addClass("SmBtmMargin").append(
                        $("<label for='editScacBox'>").css("margin-right", "10px").html("SCAC"),
                        $("<span>").css("margin-right", "10px").append(getScacSelect("editScacBox GreySelect", true))
                    ),
                    $("<div>").addClass("SmBtmMargin").append(
                        $("<label for='editBolBox'>").css("margin-right", "10px").html("Routing"),
                        $("<input>").addClass("rtBox applyBox").attr("id", "editRtBox").prop("type", "text")
                    ),
                    $("<div>").addClass("SmBtmMargin").html("<br>"),
                    $("<div>").addClass("SmBtmMargin").append(
                        $("<input type='checkbox'>").attr("id", "editAllCheckbox"),
                        $("<label for='editAllCheckbox'>").css("margin-right", "10px").html("Yes, edit " + recCt + " invoice" + plural)
                    ),
                    $("<div>").addClass("SmBtmMargin").html("<br>"),
                    $("<div>").append(
                        $("<button>").prop({ "type": "button", "id": "editAllButton" }).addClass("button").css("margin-right", "10px").html("EDIT MATCHED INVOICES").click(function () {
                            if ($("#editAllCheckbox").is(":checked")) {
                                Working.Begin();
                                var updatePack = {
                                    "filter": recordTable.GetFilter(),
                                    "data": {
                                        "bolnumber": $("#editBolBox").val(),
                                        "scaccode": $(".editScacBox").val(),
                                        "routing": $("#editRtBox").val(),
                                        "shipdate": $("#editShipDateBox").val()
                                    }
                                };
                                submitAJAXRequest("IntegrationManager.aspx/EditFilteredInvoices", updatePack, function (response) {
                                    Working.End();
                                    editWindow.close();
                                    recordTable.TriggerUpdate();
                                });
                            }
                        }),
                        $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
                    )
                );
                editWindow.show();
            }
        });
    }

    function openSubmit() {
        closeOpenWindows();

        var record = recordTable.GetCheckedRows();
        var keyList = [];

        for (var i = 0; i < record.length; i++) {
            keyList.push(record[i].key);
        }

        Working.Begin();
        submitAJAXRequest("IntegrationManager.aspx/GetSubmitStatusInv", keyList, function (response) {
            Working.End();
            var resp = JSON.parse(response.d);
            if (resp.success) {
                var iType = resp.data;
                if (iType == 'X') {
                    ShowMessage("Submit Error", "Only invoices of the same type can be submitted together. Check your filter and try again.", false);
                }
                else {
                    if (recordTable.Any(function (r) { return r["hprocessed"] !== "S"; })) {
                        ShowMessage("Submit Error", "You cannot submit records that have errors or are already submitted. Check your selection and try again.", false);
                        return;
                    }

                    var invList = recordTable.GetCheckedRows(function (r) {
                        return r.hprocessed === "S";
                    });

                    var submitFunc = function (submitType, consType, baseInv) {
                        var keyList = [];

                        for (var i = 0; i < invList.length; i++) {
                            keyList.push(invList[i]["key"]);
                        }

                        var requestSend = createSendRequestKey(false, submitType, keyList, consType, baseInv);

                        Working.Begin();
                        submitAJAXRequest("IntegrationManager.aspx/GetSendList", requestSend, function (response) {
                            Working.End();
                            var resp = JSON.parse(response.d);
                            if (resp.success) {
                                var presendRecord = resp.data;

                                if (!presendRecord.isGood) {
                                    ShowMessage("SubmitError", "Some of the submitted records have errors or are already submitted. Check your selection and try again.", false);
                                    return;
                                }

                                submitWindow = new DisplayWindow({
                                    Size: 'large'
                                });

                                if (submitType === "I") {
                                    var s = presendRecord.invPreList.length !== 1 ? "s" : "";
                                    var title = "Submitting " + presendRecord.invPreList.length + " record" + s;
                                }
                                else {
                                    var s = presendRecord.asnPreList.length !== 1 ? "s" : "";
                                    var title = "Submitting " + presendRecord.asnPreList.length + " record" + s;
                                }
                                if (presendRecord.isBad) {
                                    title += " Some records could not be sent because they had errors or were already submitted.";
                                }
                                submitWindow.Content.append($("<h2 class='RedBottomHead'>").text(title), $("<br>"));
                                submitWindow.Content.append(createSubmitWindow(presendRecord, submitType));
                                submitWindow.show();
                                applyTableStyling();
                                applyTableHeaderStyle($("#submitRecords"));
                            }
                        });
                    };

                    createSubmitOptionWindow(iType, invList.length, submitFunc);
                }
            }
            else {
                ShowMessage("Submit Error", "An unknown error occurred when attempting to submit. If the problem continues to persist, please contact EDI Options.", false);
            }
        });
    }

    function openSubmitAll() {
        closeOpenWindows();

        Working.Begin();
        submitAJAXRequest("IntegrationManager.aspx/GetSubmitStatusFlt", recordTable.GetFilter(), function (response) {
            Working.End();
            var resp = JSON.parse(response.d);
            if (resp.success) {
                var iType = resp.data;
                if (iType == 'X') {
                    ShowMessage("Submit Error", "Only invoices of the same type can be submitted together. Check your filter and try again.", false);
                }
                else {
                    var submitFunc = function (submitType, consType, baseInv) {
                        var requestSend = createSendRequestFilter(true, submitType, recordTable.GetFilter(), consType, baseInv)

                        Working.Begin();
                        submitAJAXRequest("IntegrationManager.aspx/GetSendList", requestSend, function (response) {
                            Working.End();
                            var resp = JSON.parse(response.d);
                            if (resp.success) {
                                var submitList = resp.data;
                                submitWindow = new DisplayWindow({
                                    Size: 'small'
                                });

                                if (!submitList.isGood) {
                                    ShowMessage("SubmitError", "Some of the submitted records have errors or are already submitted. Check your selection and try again.", false);
                                    return;
                                }

                                var title = "";
                                if (submitType == "I") {
                                    title = "Submitting " + submitList.invList.length + " invoice" + (submitList.invList.length == 1 ? "" : "s") + ".";
                                    if (submitList.isBad) {
                                        title += " Some records could not be sent because they had errors or were already submitted.";
                                    }
                                }
                                else if (submitType == "A") {
                                    title = "Submitting " + submitList.bolList.length + " ASN" + (submitList.bolList.length == 1 ? "" : "s") + ".";
                                    if (submitList.isBad) {
                                        title += " Some records could not be sent because they had errors or were already submitted.";
                                    }
                                }
                                else {
                                    title = "Submitting " + submitList.bolList.length + " ASN" + (submitList.bolList.length == 1 ? "" : "s") + " and associated invoices.";
                                    if (submitList.isBad) {
                                        title += " Some records could not be sent because they had errors or were already submitted.";
                                    }
                                }

                                var itemDiv = $("<div>");

                                if (submitType === "I") {
                                    itemDiv.append($("<button>").prop({ "type": "button" }).addClass("button").css("margin-right", "10px").html("INVOICES (" + (submitList.invList.length || 0) + ")").click(function () {
                                        var invWindow = new DisplayWindow({
                                            Size: 'small'
                                        });
                                        var container = $("<div>").addClass("invList").append(
                                            $("<p>").append("The following invoices are sent:"),
                                            $("<div>").css({ "max-height": "400px", "overflow": "auto" }).append(
                                                $("<p>").css("word-wrap", "break-word").append(submitList.invList.join(", "))
                                            )
                                        );

                                        var footer = $("<div>").append(
                                            $("<button>").prop({ "type": "button" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
                                        );
                                        invWindow.Content.html($("<div>").append(
                                            container,
                                            footer
                                        ));
                                        invWindow.show();
                                    }));
                                }
                                else {
                                    itemDiv.append($("<button>").prop({ "type": "button" }).addClass("button").css("margin-right", "10px").html("ASN (" + (submitList.bolList.length || 0) + ")").click(function () {
                                        var bolWindow = new DisplayWindow({
                                            Size: 'small'
                                        });
                                        var container = $("<div>").addClass("invList").append(
                                            $("<p>").append("The following BOLs used to create ASNs are sent:"),
                                            $("<div>").css({ "max-height": "400px", "overflow": "auto" }).append(
                                                $("<p>").css("word-wrap", "break-word").append(submitList.bolList.join(", "))
                                            )
                                        );

                                        var footer = $("<div>").append(
                                            $("<button>").prop({ "type": "button" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
                                        );
                                        bolWindow.Content.html($("<div>").append(
                                            container,
                                            footer
                                        ));
                                        bolWindow.show();
                                    }));
                                }

                                var packList = getPackTypeList();
                                var cartList = getCartonTypeList(packList[0]);

                                var cartSelect = getCartonTypeSelect("subAllCartType GreySelect", packList[0]);
                                if (arrayContains(cartList, options.Packing.Default)) {
                                    $("option[value='" + options.Packing.Default + "']", cartSelect).attr("selected", "selected");
                                }

                                submitWindow.Content.append(
                                    $("<h2>").addClass("RedBottomHead").html(title),
                                    $("<div>").addClass("SmBtmMargin").html("<br>"),
                                    itemDiv,
                                    $("<div>").addClass("SmBtmMargin").html("<br>"),
                                    submitType !== "I" ? $("<div>").addClass("SmBtmMargin").append(
                                        $("<span>").css("margin-right", "10px").append(getPackTypeSelect("subAllPackType GreySelect").change(function () {
                                            var plType = $(".subAllPackType").val();

                                            var cartList = getCartonTypeList(plType);
                                            var cartSelect = getCartonTypeSelect("subAllCartType GreySelect", plType);
                                            if (arrayContains(cartList, options.Packing.Default)) {
                                                $("option[value='" + options.Packing.Default + "']", cartSelect).attr("selected", "selected");
                                            }

                                            $(".cartDiv").empty();
                                            $(".cartDiv").append(cartSelect);
                                        })),
                                        $("<span>").addClass("cartDiv").append(cartSelect)
                                    ) : "",
                                    $("<div>").addClass("SmBtmMargin").html("<br>"),
                                    $("<div>").addClass("SmBtmMargin").html("<br>"),
                                    $("<div>").append(
                                        $("<button>").prop({ "type": "button", "id": "editAllButton" }).addClass("button").css("margin-right", "10px").html("SUBMIT").click(function () {
                                            var request = {
                                                isAll: true,
                                                subType: submitType,
                                                filter: recordTable.GetFilter(),
                                                baseInv: submitList.baseInv,
                                                splitType: submitList.splitType
                                            };
                                            if (submitType !== "I") {
                                                request.allPackType = $(".subAllPackType").val();
                                                request.allCartType = $(".subAllCartType").val();
                                            }
                                            Working.Begin();
                                            submitAJAXRequest("IntegrationManager.aspx/SendRecords", request, function (response) {
                                                Working.End();
                                                submitWindow.close();
                                                var resp = JSON.parse(response.d);
                                                postSendRecord(resp);
                                            });
                                        }),
                                        $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
                                    )
                                );
                                submitWindow.show();
                            }
                        });
                    };

                    createSubmitOptionWindow(iType, 0, submitFunc);
                }
            }
            else {
                ShowMessage("Submit Error", "An unknown error occurred when attempting to submit. If the problem continues to persist, please contact EDI Options.", false);
            }
        });
    }

    function openDelete() {
        closeOpenWindows();
        var deleteList = recordTable.GetCheckedRows(function (r) {
            return r["hprocessed"] === "S" || r["hprocessed"] === "X";
        });

        if (deleteList.length > 0) {
            var recCt = deleteList.length;
            var plural = recCt === 1 ? "" : "s";
            var deleteWindow = new DisplayWindow({
                Size: 'large'
            });
            deleteWindow.Content.append(
                $("<h2>").addClass("RedBottomHead").html("Remove Selected Invoices"),
                $("<div>").addClass("SmBtmMargin").html("Are you sure you want to remove " + recCt + " invoice" + plural + "?"),
                $("<div>").addClass("SmBtmMargin").html("<br>"),
                $("<div>").append(
                    $("<button>").prop({ "type": "button", "id": "removeButton" }).addClass("button").css("margin-right", "10px").html("REMOVE").click(function () {
                        var updatePack = [];
                        for (var i = 0; i < deleteList.length; i++) {
                            updatePack.push(deleteList[i]["key"]);
                        }
                        Working.Begin();
                        submitAJAXRequest("IntegrationManager.aspx/RemoveInvoices", updatePack, function (response) {
                            Working.End();
                            recordTable.TriggerUpdate();
                            deleteWindow.close();
                        });
                    }),
                    $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
                )
            );
            deleteWindow.show();
        }
        else {
            ShowMessage("Delete Error", "Unable to delete selected records. Select invoices and try again.", false);
        }
    }

    function openReset() {
        closeOpenWindows();
        var resetList = recordTable.GetCheckedRows(function (r) {
            return r["hprocessed"] === "Y";
        });

        if (resetList.length > 0) {
            var recCt = resetList.length;
            var plural = recCt === 1 ? "" : "s";
            var resetWindow = new DisplayWindow({
                Size: 'large'
            });
            resetWindow.Content.append(
                $("<h2>").addClass("RedBottomHead").html("Reset Selected Invoices"),
                $("<div>").addClass("SmBtmMargin").html("Are you sure you want to reset " + recCt + " invoice" + plural + "?"),
                $("<div>").addClass("SmBtmMargin").html("<br>"),
                $("<div>").append(
                    $("<button>").prop({ "type": "button", "id": "removeButton" }).addClass("button").css("margin-right", "10px").html("RESET").click(function () {
                        var updatePack = [];
                        for (var i = 0; i < resetList.length; i++) {
                            updatePack.push(resetList[i]["key"]);
                        }
                        Working.Begin();
                        submitAJAXRequest("IntegrationManager.aspx/ResetInvoices", updatePack, function (response) {
                            Working.End();
                            recordTable.TriggerUpdate();
                            resetWindow.close();
                        });
                    }),
                    $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
                )
            );
            resetWindow.show();
        }
        else {
            ShowMessage("Reset Error", "Unable to reset selected invoices. Select invoices, and try again.", false);
        }
    }

    function createEditWindow(editList) {
        var columnHeaders = [
            { Header: "Invoice #", Column: "invoiceno" },
            { Header: "Ship Date", Column: "shipdate" },
            { Header: "BOL #", Column: "bolnumber" },
            { Header: "SCAC", Column: "scaccode" },
            { Header: "Routing", Column: "routing" }
        ];

        var bolHead = { "title": "Edit BOL", "type": "text", "placeholder": "BOL #" };
        var rtHead = { "title": "Edit Routing", "type": "text", "placeholder": "Routing" };
        var aplHead = { "id": "bulkApplyButton", "data-tooltip": "Apply Changes" };

        var applyClick = function () {
            var blVal = $(".bolApplyBox").val();
            var rtVal = $(".rtApplyBox").val();
            var scVal = $(".scApplyBox").val();
            var invCheck = editTable.GetCheckedIndices();
            for (var i = 0; i < invCheck.length; i++) {
                if (blVal) {
                    $(".bolBox[index='" + invCheck[i] + "']").val(blVal);
                }
                if (rtVal) {
                    $(".rtBox[index='" + invCheck[i] + "']").val(rtVal);
                }
                if (scVal !== "X") {
                    $(".scCol[index='" + invCheck[i] + "']").val(scVal);
                }
            }
            return false;
        };

        var editTableOptions = {
            title: "Edit Invoices",
            isSearchEnabled: false,
            isPagination: false,
            isCriteriaEnabled: false,
            isToolbarEnabled: true,
            isCollection: true,
            columns: columnHeaders,
            rows: editList,
            replaces: {
                "bolnumber": replaceBOL,
                "scaccode": replaceSCACSelect,
                "routing": replaceRouting,
                "shipdate": replaceShipDate
            },
            TableToolbarEditButtons: function () {
                return $("<div>").addClass("applyDiv").append(
                    $("<p>").addClass("ToolbarHead").text("Bulk Change"),
                    $("<input>").addClass("bulkApplyBox bolApplyBox").attr(bolHead),
                    getScacSelect("scApplyBox GreySelect", true),
                    $("<input>").addClass("bulkApplyBox rtApplyBox").attr(rtHead),
                    $("<button>").addClass("button btnSm tooltip").css("margin-left", "10px").attr(aplHead).text("▶").click(applyClick)
                );
            }
        };

        var editTable = new PageTable(editTableOptions);

        var table = $("<div>").attr("id", "editRecords");
        editTable.Create(table);

        var container = $("<div>");

        var editFunc = function () {
            var updatePack = [];
            var boxCol = $(".bolBox");
            var rtCol = $(".rtBox");
            var scCol = $(".scCol");
            var dtCol = $(".dtCol");
            for (var i = 0; i < boxCol.length; i++) {
                var bVal = $(boxCol[i]).val();
                var rVal = $(rtCol[i]).val();
                var sVal = $(scCol[i]).val();
                var dVal = $(dtCol[i]).val();
                var bIndex = $(boxCol[i]).attr("index");
                if (bVal !== "") {
                    var data = {
                        key: editList[bIndex].key,
                        bolnumber: bVal,
                        scaccode: sVal,
                        routing: rVal,
                        shipdate: dVal
                    };
                    updatePack.push(data);
                }
            }
            Working.Begin();
            submitAJAXRequest("IntegrationManager.aspx/EditInvoices", updatePack, function (response) {
                Working.End();
                recordTable.TriggerUpdate();
                editWindow.close();
            });
        }

        var footer = $("<div>").addClass("submitDiv").append(
            $("<button>").prop({ "type": "button", "id": "submitButton" }).addClass("button").css("margin-right", "10px").html("COMMIT CHANGES").click(function () {
                editFunc(false);
            }),
            $("<button>").prop({ "type": "button", "id": "closeButton" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
        );

        container.append(table, footer);
        return container;
    }

    function createSubmitOptionWindow(xfertype, invCount, submitFunc) {
        var isSubmitAll = invCount <= 0;
        var defaultXType = xfertype === InvTypeConsolidated ? XTypeMerge : XTypeKeep;
        if (isNone()) {
            ShowMessage("Submit Error", "Unable to create invoices or ASNs due to a configuration issue. Please contact EDI Options.", false);
            return;
        }
        if (isAsnOnly()) {
            submitFunc("A", "X", "");
            return;
        }
        if (isInvOnly() && (xfertype == InvTypeMixed || xfertype == InvTypePrePacked)) {
            submitFunc("I", "X", "");
            return;
        }

        var consOnFunc = function () {
            if (this.checked) {
                $("#isSplitDiv").show();
            }
        }

        var consOffFunc = function () {
            if (this.checked) {
                $("#isSplitDiv").hide();
            }
        }

        var subOptionWindow = new DisplayWindow({
            Size: 'small'
        });

        var headDiv = $("<div>").append($("<h2>").addClass("RedBottomHead").html(isSubmitAll ? "Submitting ALL filtered invoices" :
            "Submitting " + invCount + " invoice" + (invCount == 1 ? "" : "s")
        ));

        if (isSubmitAll) {
            headDiv.append(
                $("<p class='info'>").text("To submit individual invoices, check each one and click 'Submit' again.")
            );
        }

        var choiceDiv = $("<div>");
        if (isBoth()) {
            choiceDiv.append(
                $("<div>").addClass("SmBtmMargin").html("What type of submission would you like to do?"),
                $("<div>").append(
                    $("<input>").prop({ "name": "subType", "type": "radio", "id": "subInv", "value": "I", "checked": "checked" }).change(consOnFunc),
                    $("<label>").prop({ "for": "subInv" }).text("Invoices Only")
                ),
                $("<div>").append(
                    $("<input>").prop({ "name": "subType", "type": "radio", "id": "subAsn", "value": "A" }).change(consOffFunc),
                    $("<label>").prop({ "for": "subAsn" }).text("ASNs Only")
                ),
                $("<div>").append(
                    $("<input>").prop({ "name": "subType", "type": "radio", "id": "subBoth", "value": "B" }).change(consOnFunc),
                    $("<label>").prop({ "for": "subBoth" }).text("Invoices and ASNs")
                ),
                $("<div>").addClass("SmBtmMargin").html("<br>")
            );
        }

        var optDiv = $("<div>").prop("id", "isSplitDiv");
        if (xfertype === InvTypeDistributed) {
            optDiv.append(
                $("<div>").addClass("SmBtmMargin").html("How would you like to submit your invoices?"),
                $("<div>").append(
                    $("<input>").prop({ "name": "consType", "type": "radio", "value": "X", "id": "optNone", "checked": "checked" }),
                    $("<label>").prop({ "for": "optNone" }).text("As Is")
                ),
                $("<div>").append(
                    $("<input>").prop({ "name": "consType", "type": "radio", "value": "S", "id": "optSplit" }),
                    $("<label>").prop({ "for": "optSplit" }).text("One Invoice Per Store")
                ),
                $("<div>").addClass("SmBtmMargin").html("<br>")
            );
        }
        else if (xfertype === InvTypeConsolidated) {
            optDiv.append(
                $("<div>").append(
                    $("<label>").text("Consolidate invoices using base invoice "),
                    $("<input>").prop({ "type": "text", "id": "mergeBaseInvBox" })
                ),
                $("<div>").addClass("SmBtmMargin").html("<br>")
            );
        }

        subOptionWindow.Content.append(
            headDiv,
            choiceDiv,
            optDiv,
            $("<div>").append(
                $("<button>").prop({ "type": "button", "id": "submitButton" }).addClass("button").css("margin-right", "10px").html("OK").click(function () {
                    var submitType = "I";
                    if (isAsnOnly()) {
                        submitType = "A";
                    }
                    else if (isBoth()) {
                        submitType = $("input[name='subType']:checked").val();
                    }
                    var consType = submitType === "A" ? XTypeKeep : defaultXType;
                    if (xfertype == InvTypeDistributed) {
                        consType = $("input[name='consType']:checked").val();
                    }

                    var baseInv = $("#mergeBaseInvBox").val() || "";

                    if (consType == "M" && submitType !== "A") {
                        if (baseInv == "") {
                            ShowMessage("Submit Error", "Enter a base invoice number.", false);
                            return;
                        }
                        else if (baseInv.length > 18) {
                            ShowMessage("Submit Error", "The maximum length for a base invoice number is 18 characters.", false);
                            return;
                        }
                    }
                    subOptionWindow.close();
                    submitFunc(submitType, consType, baseInv);
                }),
                $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
            )
        );
        subOptionWindow.show();
    }

    function createSubmitWindow(presendRecord, submitType) {

        var tableInv = $("<div>").attr("id", "subInv");
        var tableAsn = $("<div>").attr("id", "subAsn");
        var tableCon = $("<div>").attr("id", "subCon");

        var isInvSubmit = submitType === "I";
        var isCons = presendRecord.splitType === XTypeMerge;
        var subInvTable;
        var subAsnTable;
        if (isCons) {
            var subConTable = createSubTableInvCons(presendRecord.consList);
            subConTable.Create(tableCon);
        }

        if (!isInvSubmit) {
            var subList = [];
            for (var i = 0; i < presendRecord.asnPreList.length; i++) {
                var slItem = { pltype: getPackTypeList()[0] };
                $.extend(true, slItem, presendRecord.asnPreList[i]);
                subList.push(slItem);
            }

            var subAsnOptions = {
                title: "ASNs",
                isSearchEnabled: false,
                isPagination: false,
                isCriteriaEnabled: false,
                columns: [
                    { Header: "Invoice Numbers", Column: "invList" },
                    { Header: "BOL #", Column: "bolnumber" },
                    { Header: "Ship To ID", Column: "stid" },
                    { Header: "Pack Type", Column: "pltype" },
                    { Header: "Carton Distribution", Column: "mt" }
                ],
                rows: subList,
                replaces: {
                    "invList": replaceInvList,
                    "mt": function (index, row, isInit) {
                        if (isInit) {
                            var cartOpt = getCartonTypeList(row["pltype"]);

                            if (arrayContains(cartOpt, options.Packing.Default)) {
                                row["mt"] = options.Packing.Default;
                            }
                            else if (!arrayContains(cartOpt, row["mt"])) {
                                row["mt"] = cartOpt[0];
                            }
                        }

                        var cartonSelect = $("<td>").append(
                            getCartonTypeSelect("mgCol GreySelect", row["pltype"])
                        );

                        $("option[value='" + row["mt"] + "']", cartonSelect).attr("selected", "selected");

                        $(cartonSelect).change(function () {
                            row["mt"] = $(".mgCol", cartonSelect).val();
                            subAsnTable.TriggerUpdate();
                        });
                        return cartonSelect;
                    },
                    "pltype": function (index, row, isInit) {
                        var packSelect = $("<td>").append(
                            getMergePackTypeSelect("plCol GreySelect", row["isDistributed"])
                        );

                        $("option[value='" + row["pltype"] + "']", packSelect).attr("selected", "selected");

                        $(packSelect).change(function () {
                            row["pltype"] = $(".plCol", packSelect).val();
                            subAsnTable.TriggerUpdate();
                        });
                        return packSelect;
                    }
                },
                onFilterUpdate: function () {
                    $("#subAsn").empty();
                    subAsnTable.Create($("#subAsn"));
                }
            };

            subAsnTable = new PageTable(subAsnOptions);

            subAsnTable.Create(tableAsn);
        }
        else if (!isCons) {
            subInvTable = createSubTableInv(presendRecord.invPreList);
            subInvTable.Create(tableInv);
        }

        var container = $("<div>");

        var footer = $("<div>").addClass("submitDiv").append(
            $("<button>").prop({ "type": "button", "id": "submitButton" }).addClass("button").css("margin-right", "10px").html("SUBMIT").click(function () {
                var toSend;
                if (isInvSubmit) {
                    toSend = isCons ? presendRecord.invPreList : subInvTable.GetRows();
                }
                else {
                    toSend = subAsnTable.GetRows();
                }
                var request = {
                    isAll: false,
                    subType: submitType,
                    keyList: [],
                    mergeList: [],
                    allPackType: "",
                    allCartType: "",
                    splitType: presendRecord.splitType,
                    baseInv: presendRecord.baseInv
                };
                for (var i = 0; i < toSend.length; i++) {
                    if (isInvSubmit) {
                        request.keyList.push(toSend[i]["key"]);
                    }
                    else {
                        request.mergeList.push({ bolnumber: toSend[i]["bolnumber"], cartType: toSend[i]["mt"], packType: toSend[i]["pltype"] });
                    }
                }
                Working.Begin();
                submitAJAXRequest("IntegrationManager.aspx/SendRecords", request, function (response) {
                    Working.End();
                    submitWindow.close();
                    var resp = JSON.parse(response.d);
                    postSendRecord(resp);
                });
            }),
            $("<button>").prop({ "type": "button", "id": "closeButton" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
        );

        container.append(tableInv, tableCon, tableAsn, footer);
        return container;
    }

    function createSubTableInv(submitList) {
        var subList = [];
        for (var i = 0; i < submitList.length; i++) {
            var slItem = {};
            $.extend(true, slItem, submitList[i]);
            subList.push(slItem);
        }

        return new PageTable({
            title: "Invoices",
            isSearchEnabled: false,
            isPagination: false,
            isCriteriaEnabled: false,
            columns: [
                { Header: "Invoice #", Column: "invoiceno" },
                { Header: "BOL #", Column: "bolnumber" },
                { Header: "Ship To ID", Column: "stid" }
            ],
            rows: subList
        });
    }

    function createSubTableInvCons(submitList) {
        var subList = [];
        for (var i = 0; i < submitList.length; i++) {
            var slItem = {};
            $.extend(true, slItem, submitList[i]);
            subList.push(slItem);
        }

        return new PageTable({
            title: "Consolidated Invoices",
            isSearchEnabled: false,
            isPagination: false,
            isCriteriaEnabled: false,
            columns: [
                { Header: "Master PO #", Column: "releasenum" },
                { Header: "Invoice Numbers", Column: "invList" },
            ],
            rows: subList,
            replaces: {
                "invList": replaceInvList,
            }
        });
    }

    function postSendRecord(resp) {
        if (resp.success) {
            if(resp.data.length > 0) {
                validatePL(resp.data);
            }
        }
        else {
            ShowMessage("Submit Error", "An unknown error occurred when attempting to submit. If the problem continues to persist, please contact EDI Options.", false);
        }
        recordTable.TriggerUpdate();
    }

    function validatePL(keyList)
    {
        Working.Begin();
        PostToOC("PLVALIDATE", { TrxList: keyList }, function (data) {
            Working.End();
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

    // -- Replace Options --

    function replaceBOL(index, row) {
        if (row["hprocessed"] === "Y") {
            return row["bolnumber"];
        }
        else {
            return $("<input>").addClass("bolBox applyBox").prop("type", "text").attr("index", index).val(row["bolnumber"]);
        }
    }

    function replaceRouting(index, row) {
        if (row["hprocessed"] === "Y") {
            return row["routing"];
        }
        else {
            return $("<input>").addClass("rtBox applyBox").prop("type", "text").attr("index", index).val(row["routing"]);
        }
    }

    function replaceSCACSelect(index, row) {
        if (row["hprocessed"] === "Y") {
            return getScacDesc(row["scaccode"]);
        }
        else {
            var scacSelect = $("<td>");
            if (arrayContains(getScacCodeList(), row["scaccode"])) {
                scacSelect.append(
                    getScacSelect("scCol GreySelect").attr("index", index)
                );
                $("option[value='" + row["scaccode"] + "']", scacSelect).attr("selected", "selected");
            }
            else {
                scacSelect.append(
                    getScacSelect("scCol GreySelect", true, row["scaccode"]).attr("index", index)
                );
            }
            return scacSelect;
        }
    }

    function replacePLSelect(index, row) {
        if (row["hprocessed"] === "Y") {
            switch (row["pltype"]) {
                case PackTypePrepacked:
                    return "Prepacked";
                case PackTypeMixed:
                    return "Mixed";
                case PackTypeDistributed:
                    return "Distributed";
                default:
                    return "Unknown";
            }
        }
        else {
            var packSelect = $("<td>").append(
                getPackTypeSelect("plCol GreySelect")
            );
            if (arrayContains(getPackTypeList(), row["pltype"])) {
                $("option[value='" + row["pltype"] + "']", packSelect).attr("selected", "selected");
            }
            return packSelect;
        }
    }

    function replaceShipDate(index, row) {
        if (row["hprocessed"] === "Y") {
            return row["shipdate"];
        }
        else {
            var elem = $('<input type="text">').addClass("dtCol").css('width', '100px').datepicker({
                dateFormat: 'M dd yy'
            });
            if (isDate(row["shipdate"])) {
                elem.val(row["shipdate"]);
            }
            else {
                elem.val(options.Date);
            }
            return elem;
        }
    }

    function replaceInvList(index, row) {
        if (row["invList"].length > 1) {
            return $("<button>").prop({ "type": "button", "id": "submitButton" }).addClass("button").css("margin-right", "10px").html("INVOICES (" + (row["invList"].length || 0) + ")").click(function () {
                var invWindow = new DisplayWindow({
                    Size: 'small'
                });
                var container = $("<div>").addClass("invList").append(
                    $("<p>").append("The following invoices are merged:"),
                    $("<div>").css({ "max-height": "400px", "overflow": "auto" }).append(
                        $("<p>").css("word-wrap", "break-word").append(row["invList"].join(", "))
                    )
                );

                var footer = $("<div>").addClass("submitDiv").append(
                    $("<button>").prop({ "type": "button" }).addClass("button CloseWindowBtn").html("CLOSE WINDOW")
                );
                invWindow.Content.html($("<div>").append(
                    container,
                    footer
                ));
                invWindow.show();
            });
        }
        else {
            return $("<span>").text(row["invList"].toString());
        }
    }

    function replaceProcFlag(index, row) {
        var statusDiv = $("<div>");
        var statusIcon = $("<span>").addClass("SmBtmMargin")
        statusDiv.append(statusIcon);
        switch (row["hprocessed"]) {
            case "Y":
                statusIcon.addClass("good");
                statusDiv.append("Processed");
                break;
            case "X":
                statusIcon.addClass("warning");
                statusDiv.append("Error");
                break;
            default:
                statusIcon.addClass("neutral");
                statusDiv.append("Pending");
                break;
        }
        return statusDiv;
    }

    function replaceMsg(index, row) {
        var mdiv = $("<td>");
        var msg = row["msg"];
        if ((typeof msg === 'string' || msg instanceof String) && msg !== "") {
            mdiv.append($("<button>").prop({ "type": "button", "index": index }).addClass("button").html("VIEW").click(function () {
                ShowMessage("Invoice Error", msg, false);
                return false;
            }));
        }
        else {
            mdiv.append($("<span>").text("    "));
        }
        return mdiv;
    }

    // -- End Replace Options --

    // -- Utility --

    function getScacDesc(code) {
        for (var i = 0; i < options.Shipping.Carriers.length; i++) {
            if (code === options.Shipping.Carriers[i]["scaccode"]) {
                return options.Shipping.Carriers[i]["scacdesc"];
            }
        }
        return code;
    }

    function getScacCodeList() {
        var scArr = [];
        for (var i = 0; i < options.Shipping.Carriers.length; i++) {
            scArr.push(options.Shipping.Carriers[i]["scaccode"]);
        }
        return scArr;
    }

    function getScacSelect(classAttr, includeDefault, defaultField) {
        var scacSelect = $("<select>").addClass(classAttr);
        if (includeDefault) {
            var option = "Same as before";
            var val = "X";
            if (defaultField) {
                option = defaultField + ": Unknown";
                val = defaultField;
            }
            scacSelect.append($("<option>").attr("value", val).html(option));
        }
        for (var i = 0; i < options.Shipping.Carriers.length; i++) {
            var code = options.Shipping.Carriers[i]["scaccode"];
            var desc = options.Shipping.Carriers[i]["scacdesc"];
            scacSelect.append($("<option>").attr("value", code).html(code + ": " + desc));
        }
        return scacSelect;
    }

    function getPackTypeList() {
        var plArr = [];
        if (options.Packing.IsPP) {
            plArr.push(PackTypePrepacked);
        }
        if (options.Packing.IsMX) {
            plArr.push(PackTypeMixed);
        }
        if (options.Packing.IsDS) {
            plArr.push(PackTypeDistributed);
        }
        return plArr;
    }

    function getPackTypeSelect(classAttr, includeDefault) {
        var packSelect = $("<select>").addClass(classAttr);
        if (includeDefault) {
            packSelect.append($("<option>").attr("value", PackTypeDefault).html("Same as before"));
        }
        if (options.Packing.Enabled) {
            if (options.Packing.IsPP) {
                packSelect.append($("<option>").attr("value", PackTypePrepacked).html("Prepacked"));
            }
            if (options.Packing.IsMX) {
                packSelect.append($("<option>").attr("value", PackTypeMixed).html("Mixed"));
            }
            if (options.Packing.IsDS) {
                packSelect.append($("<option>").attr("value", PackTypeDistributed).html("Distributed"));
            }
        }
        return packSelect;
    }

    function getCartonTypeList(plType) {
        switch (plType) {
            case PackTypePrepacked:
            default:
                return [CartTypeManual, CartTypeAutomatic];
            case PackTypeMixed:
                return [CartTypeManual, CartTypePerASN, CartTypePerPO];
            case PackTypeDistributed:
                return [CartTypePerPOStore];
        }
    }

    function getCartonTypeSelect(classAttr, plType) {
        switch (plType) {
            case PackTypePrepacked:
            default:
                return $("<select>").addClass(classAttr).append(
                    $("<option>").attr("value", CartTypeManual).html("Manual"),
                    $("<option>").attr("value", CartTypeAutomatic).html("Automatic")
                );
            case PackTypeMixed:
                return $("<select>").addClass(classAttr).append(
                    $("<option>").attr("value", CartTypeManual).html("One box per PO and Item"),
                    $("<option>").attr("value", CartTypePerASN).html("One box per ASN"),
                    $("<option>").attr("value", CartTypePerPO).html("One box per PO")
                );
            case PackTypeDistributed:
                return $("<select>").addClass(classAttr).append(
                    $("<option>").attr("value", CartTypePerPOStore).html("One box per PO and Store")
                );
        }
    }

    function getMergePackTypeSelect(classAttr, isDistributed) {
        if (isDistributed) {
            return $("<select>").addClass(classAttr).append(
                $("<option>").attr("value", PackTypeDistributed).html("Distributed")
            );
        }
        else {
            return getPackTypeSelect(classAttr);
        }
    }

    // -- End Utility --

}());