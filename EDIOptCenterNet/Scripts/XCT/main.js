(function () {
    var xrefTable;
    var editWindow;

    $(function () {
        Working.Begin();
        submitAJAXRequest("CatalogXref.aspx/InitXrefList", "", function (response) {
            Working.End();
            var resp = JSON.parse(response.d);
            if (resp.success) {
                resp = resp.data;
                var mainTable = $("#mainDiv");
                xrefTable = new PageTable(CreateMainTableOptions(resp));
                xrefTable.SetFilter(resp.TableState.Filter);
                mainTable.empty();
                xrefTable.Create(mainTable);
            }
        });
    });

    function CreateMainTableOptions(resp) {
        return {
            title: "Catalog Cross Reference",
            isAdvancedSearch: true,
            isCollection: true,
            isCriteriaEnabled: false,
            columns: resp.TableState.Columns,
            rows: resp.TableData,
            onFilterUpdate: function (filterInfo) {
                Working.Begin();
                submitAJAXRequest("CatalogXref.aspx/GetXrefList", filterInfo, function (response) {
                    Working.End();
                    var resp = JSON.parse(response.d);
                    if (resp.success) {
                        resp = resp.data;
                        var mainTable = $("#mainDiv");
                        xrefTable.SetFilter(resp.TableState.Filter);
                        xrefTable.SetRows(resp.TableData);
                        xrefTable.SetColumns(resp.TableState.Columns);
                        mainTable.empty();
                        xrefTable.Create(mainTable);
                    }
                });
            },
            TableToolbarEditButtons: function () {
                return $("<div>").append(
                    $("<div>").addClass("createDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Create"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarCreateBtn tooltip").attr("data-tooltip", "Create Cross Reference").click(openAdd)
                        )
                    ),
                    $("<div>").addClass("editDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Edit"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarEditHeadBtn tooltip").attr("data-tooltip", "Edit Cross Reference").click(openEdit)
                        )
                    ),
                    $("<div>").addClass("deleteDiv").append(
                        $("<p>").addClass("ToolbarHead").text("Delete"),
                        $("<ul>").addClass("toolbar-icons").append(
                            $("<li>").addClass("ToolbarDeleteBtn tooltip").attr("data-tooltip", "Delete Cross Reference").click(openRemove)
                        )
                    )
                );
            }
        };
    }

    function openAdd() {
        editWindow = new DisplayWindow({
            Size: 'small'
        });

        var lblStyle = {"margin-right":"10px","width":"150px","display":"inline-block"};
        editWindow.Content.append(
            $("<h2>").addClass("RedBottomHead").html("Create New Cross Reference"),
            $("<div>").addClass("SmBtmMargin").append(
                $("<label for='vendNameBox'>").css(lblStyle).html("Company Name"),
                $("<input>").attr({ "id": "vendNameBox", "maxLength": 60 }).prop("type", "text")
            ),
            $("<div>").addClass("SmBtmMargin").append(
                $("<label for='vendIdBox'>").css(lblStyle).html("GXS Account"),
                $("<input>").attr({ "id": "vendIdBox", "maxLength": 15 }).prop("type", "text")
            ),
            $("<div>").addClass("SmBtmMargin").append(
                $("<label for='vendSeqBox'>").css(lblStyle).html("Selection Code"),
                $("<input>").attr({ "id": "vendSeqBox", "maxLength": 3 }).prop("type", "text")
            ),
            $("<div>").addClass("SmBtmMargin").append(
                $("<label for='brandNameBox'>").css(lblStyle).html("Brand Name"),
                $("<input>").attr({ "id": "brandNameBox", "maxLength": 80 }).prop("type", "text")
            ),
            $("<div>").addClass("SmBtmMargin").html("<br>"),
            $("<div>").append(
                $("<button>").prop({ "type": "button", "id": "editAllButton" }).addClass("button").css("margin-right", "10px").html("CREATE").click(function () {
                    var updatePack = {
                        "vendorname": $("#vendNameBox").val(),
                        "vendorid": $("#vendIdBox").val(),
                        "vendorseq": $("#vendSeqBox").val(),
                        "brandname": $("#brandNameBox").val()
                    };
                    Working.Begin();
                    submitAJAXRequest("CatalogXref.aspx/CreateXref", updatePack, function (response) {
                        Working.End();
                        editWindow.close();
                        xrefTable.TriggerUpdate();
                    });
                }),
                $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
            )
        );
        editWindow.show();
    }

    function openEdit() {
        var editList = xrefTable.GetCheckedRows();

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
            ShowMessage("Edit Error", "Unable to edit selected cross references. Select cross references, and try again.", false);
        }
    }

    function openRemove() {
        var deleteList = xrefTable.GetCheckedRows();

        if (deleteList.length > 0) {
            var recCt = deleteList.length;
            var plural = recCt === 1 ? "" : "s";
            var deleteWindow = new DisplayWindow({
                Size: 'small'
            });
            deleteWindow.Content.append(
                $("<h2>").addClass("RedBottomHead").html("Remove Selected Cross References"),
                $("<div>").addClass("SmBtmMargin").html("Are you sure you want to remove " + recCt + " cross reference" + plural + "?"),
                $("<div>").addClass("SmBtmMargin").html("<br>"),
                $("<div>").append(
                    $("<button>").prop({ "type": "button", "id": "removeButton" }).addClass("button").css("margin-right", "10px").html("REMOVE").click(function () {
                        var updatePack = [];
                        for (var i = 0; i < deleteList.length; i++) {
                            updatePack.push(deleteList[i]["key"]);
                        }
                        Working.Begin();
                        submitAJAXRequest("CatalogXref.aspx/RemoveXref", updatePack, function (response) {
                            Working.End();
                            xrefTable.TriggerUpdate();
                            deleteWindow.close();
                        });
                    }),
                    $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
                )
            );
            deleteWindow.show();
        }
        else {
            ShowMessage("Delete Error", "Unable to delete selected records. Select cross references and try again.", false);
        }
    }

    function createEditWindow(editList) {
        var columnHeaders = [
            { Header: "Company Name", Column: "vendorname" },
            { Header: "Gxs Account", Column: "vendorid" },
            { Header: "Selection Code", Column: "vendorseq" },
            { Header: "Brand Name", Column: "brandname" }
        ];

        var editTableOptions = {
            title: "Edit Cross References",
            isSearchEnabled: false,
            isPagination: false,
            isCriteriaEnabled: false,
            columns: columnHeaders,
            rows: editList,
            replaces: {
                "vendorname": replaceVendName,
                "vendorid": replaceVendId,
                "vendorseq": replaceVendSeq,
                "brandname": replaceBrandName
            }
        };

        var editTable = new PageTable(editTableOptions);

        var table = $("<div>").attr("id", "editRecords");
        editTable.Create(table);

        var container = $("<div>");

        var editFunc = function () {
            var updatePack = [];
            var vnCol = $(".vnBox");
            var viCol = $(".viBox");
            var vsCol = $(".vsBox");
            var bnCol = $(".bnBox");
            for (var i = 0; i < vnCol.length; i++) {
                var nVal = $(vnCol[i]).val();
                var iVal = $(viCol[i]).val();
                var sVal = $(vsCol[i]).val();
                var bVal = $(bnCol[i]).val();
                var bIndex = $(vnCol[i]).attr("index");
                if (bVal !== "" && nVal != "" && iVal != "" && sVal != "") {
                    var data = {
                        key: editList[bIndex].key,
                        vendorname: nVal,
                        vendorid: iVal,
                        vendorseq: sVal,
                        brandname: bVal
                    };
                    updatePack.push(data);
                }
            }
            Working.Begin();
            submitAJAXRequest("CatalogXref.aspx/EditXref", updatePack, function (response) {
                Working.End();
                xrefTable.TriggerUpdate();
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

    function replaceVendName(index, row) {
        return $("<input>").addClass("vnBox").prop("type", "text").attr({ "index": index, "maxLength": 60 }).val(row.vendorname).keypress(function (e) {
            if (e.keyCode == 8) { return true; }
            return this.value.length < $(this).attr("maxLength");
        });
    }

    function replaceVendId(index, row) {
        return $("<input>").addClass("viBox").prop("type", "text").attr({ "index": index, "maxLength": 15 }).val(row.vendorid).keypress(function (e) {
            if (e.keyCode == 8) { return true; }
            return this.value.length < $(this).attr("maxLength");
        });
    }

    function replaceVendSeq(index, row) {
        return $("<input>").addClass("vsBox").prop("type", "text").attr({ "index": index, "maxLength": 3 }).val(row.vendorseq).keypress(function (e) {
            if (e.keyCode == 8) { return true; }
            return this.value.length < $(this).attr("maxLength");
        });
    }

    function replaceBrandName(index, row) {
        return $("<input>").addClass("bnBox").prop("type", "text").attr({ "index": index, "maxLength": 80 }).val(row.brandname).keypress(function (e) {
            if (e.keyCode == 8) { return true; }
            return this.value.length < $(this).attr("maxLength");
        });
    }
}());