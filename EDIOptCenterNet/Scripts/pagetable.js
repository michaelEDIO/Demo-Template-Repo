function PageTable(options) {
    var isInit = true;
    
    var filterInfo = {
        CurrentPage: 1,
        MaxPage: 1,
        SortColumn: "",
        SortIsDesc: false,
        SearchColumn: "",
        SearchQuery: "",
        ResultCount: 0,
        Refinements: []
    };

    var tableInfo = {
        containerID: genKey(),
        tableID: genKey(),
        title: "",
        tooltip: "",
        isToolbarEnabled: false,
        isTallTable: false,
        isAdvancedSearch: false,
        isCollection: false,
        isSearchEnabled: true,
        isPagination: true,
        isCriteriaEnabled: true,
        isCriteriaDefaultOpen: false,
        criteriaColumnCount: 0,
        isNotesEnabled: false,
        isRefine: false,
        columns: [],
        rows: [],
        replaces: { },
        onFilterUpdate: function () { },
        TableToolbarEditButtons: function () { },
        TableNotesContents: function () { },
        TableBeforeBottomButtons: function () { },
        TableBottomButtons: function () { },
        TableAfterContent: function () { }
    };

    this.TriggerUpdate = function () {
        isInit = false;
        tableInfo.onFilterUpdate(filterInfo);
    }

    this.SetFilter = function (filter) {
        if (filter) {
            for (var prop in filterInfo) {
                if (filterInfo.hasOwnProperty(prop) && filter.hasOwnProperty(prop)) {
                    filterInfo[prop] = filter[prop];
                }
            }
        }
    }

    this.GetFilter = function () {
        if (filterInfo) {
            var obj = { 
            };

            return $.extend(true, obj, filterInfo);
        }
        else {
            return {
                CurrentPage: 1,
                MaxPage: 1,
                SortColumn: "",
                SortIsDesc: false,
                SearchColumn: "",
                SearchQuery: "",
                ResultCount: 0,
                Refinements: []
            };
        }
    }

    this.SetRows = function (rows) {
        tableInfo.rows = rows;
    }

    this.AddRow = function (row) {
        if (typeof row == "object") {
            tableInfo.rows.push(row);
        }
    }

    this.GetRows = function (rowFilter) {
        if (typeof rowFilter !== "function") {
            rowFilter = function (row) { return row; }
        }
        var returned = [];
        for (var i = 0; i < tableInfo.rows.length; i++) {
            var rowObj = $.extend(true, {}, tableInfo.rows[i]);
            returned.push(rowFilter(rowObj));
        }
        return returned;
    }

    this.SetColumns = function (columnList) {
        if (Object.prototype.toString.call(columnList) === '[object Array]') {
            tableInfo.columns = [];
            for (var i = 0; i < columnList.length; i++) {
                tableInfo.columns.push($.extend(true, {}, columnList[i]));
            }
        }
    }

    this.Where = function (condition) {
        if (typeof condition !== "function") {
            condition = function (row) { return false; }
        }
        var returned = [];
        for (var i = 0; i < tableInfo.rows.length; i++) {
            var rowObj = $.extend(true, {}, tableInfo.rows[i]);
            if (condition(rowObj)) {
                returned.push(rowObj);
            }
        }
        return returned;
    }

    this.GetCheckedRowCount = function () {
        if (tableInfo.isCollection) {
            return jqElemSelect("input.RowSelect:checked").length;
        }
        else {
            return 0;
        }
    }

    this.Any = function (predicate) {
        if (typeof predicate !== "function") {
            return false;
        }

        if (tableInfo.isCollection) {
            var rows = jqElemSelect("input.RowSelect:checked");
            for (var i = 0; i < rows.length; i++) {
                var ind = rows[i].parentNode.parentNode.getAttribute("rowindex");
                if (_test(ind, predicate)) {
                    return true;
                }
            }
        }
        else {
            for (var i = 0; i < tableInfo.rows.length; i++) {
                if (_test(i, predicate)) {
                    return true;
                }
            }
        }
        return false;
    }

    var _test = function (i, predicate) {
        var rowObj = $.extend(true, {}, tableInfo.rows[i]);
        return predicate(rowObj);
    }

    this.GetCheckedRows = function (rowFilter) {
        if (typeof rowFilter !== "function") {
            rowFilter = function (x) { return true; }
        }
        if (tableInfo.isCollection) {
            var returned = [];
            var rows = jqElemSelect("input.RowSelect:checked");
            for (var i = 0; i < rows.length; i++) {
                var ind = rows[i].parentNode.parentNode.getAttribute("rowindex");
                var rowObj = $.extend(true, {}, tableInfo.rows[ind]);
                if (rowFilter(rowObj)) {
                    returned.push(rowObj);
                }
            }
            return returned;
        }
        else {
            return this.GetRows();
        }
    }

    this.GetCheckedIndices = function () {
        if (typeof rowFilter !== "function") {
            rowFilter = function (x) { return true; }
        }
        if (tableInfo.isCollection) {
            var returned = [];
            var rows = jqElemSelect("input.RowSelect:checked");
            for (var i = 0; i < rows.length; i++) {
                var ind = rows[i].parentNode.parentNode.getAttribute("rowindex");
                var rowObj = $.extend(true, {}, tableInfo.rows[ind]);
                if (rowFilter(rowObj)) {
                    returned.push(parseInt(ind));
                }
            }
            return returned;
        }
        else {
            return [];
        }
    }

    this.SetTableData = function (columns, rows) {
        tableInfo.columns = columns;
        tableInfo.rows = rows;
    }

    this.SetOptions = function (options) {
        if (options) {
            for (var prop in tableInfo) {
                if (tableInfo.hasOwnProperty(prop) && typeof(options[prop]) !== 'undefined') {
                    tableInfo[prop] = options[prop];
                }
            }
        }
    }

    this.SetReplaces = function (replaceObj) {
        if (replaceObj) {
            tableInfo.replaces = replaceObj;
        }
    }

    this.AddReplace = function (headerKey, onValue) {
        if (typeof (headerKey) == "string" && typeof (onValue) == "function") {
            tableInfo.replaces[headerKey] = onValue;
        }
    }

    this.ClearReplaces = function () {
        for (var key in tableInfo.replaces) {
            if (tableInfo.replaces.hasOwnProperty(key)) {
                delete tableInfo.replaces[key];
            }
        }
    }

    this.Create = function (jqContainer) {
        var ret = $("<div>").attr("id", tableInfo.containerID).append(
            TableTitle(tableInfo.heading),
            TableToolbar(),
            TableCriteriaDetails(),
            TableForm(),
            tableInfo.TableAfterContent(),
            TableNotes()
        );
        if (jqContainer) {
            jqContainer.empty();
            jqContainer.append(ret);
            applyTableStyling($("#" + tableInfo.containerID));
            applyTableHeaderStyle($("#" + tableInfo.containerID));
        }
        else {
            return ret;
        }
    }

    this.GetContainerID = function () {
        return tableInfo.containerID;
    }

    this.GetTableID = function () {
        return tableInfo.tableID;
    }

    var defaultReplace = function (val) {
        if (val !== "") {
            return val;
        }
        else {
            return "\u00A0\u00A0\u00A0\u00A0";
        }
    }

    var onBasicSearch = function () {
        var query = jqElemSelect("input.BasicSearchQuery").val();
        if (query) {
            filterInfo.SearchQuery = query;
            filterInfo.CurrentPage = 1;
            tableInfo.onFilterUpdate(filterInfo);
        }
    }

    var onAdvancedSearch = function () {
        var query = jqElemSelect("input.SearchQuery").val();
        if (query) {
            filterInfo.SearchQuery = query;
            filterInfo.SearchColumn = jqElemSelect("select.SearchField").val();
            filterInfo.CurrentPage = 1;
            tableInfo.onFilterUpdate(filterInfo);
        }
    }

    var onCheckCountChanged = function () {
        var checkCount = jqElemSelect("input.RowSelect:checked").length;
        jqElemSelect("li.CollCount").text(checkCount);
    }

    var onCollCheckAllClick = function () {
        if (this.checked) {
            jqElemSelect("input.RowSelectAll").prop("checked", true);
            jqElemSelect("input.RowSelect:visible").prop("checked", true);
            jqElemSelect("input.RowSelect:hidden").prop("checked", false);
        }
        else {
            jqElemSelect("input.RowSelectAll").prop("checked", false);
            jqElemSelect("input.RowSelect:visible").prop("checked", false);
        }
        onCheckCountChanged();

    }

    var onCollCheckClick = function () {
        onCheckCountChanged();
    }

    var jqElemSelect = function (selector) {
        return $("#" + tableInfo.containerID + " " + selector);
    }

    var createColumnLabel = function (filterInfo, colDesc, onFilterUpdate) {
        return function () {
            if (colDesc.IsSortable) {
                if (filterInfo.SortColumn == colDesc.Column) {
                    filterInfo.SortIsDesc = !filterInfo.SortIsDesc;
                }
                else {
                    filterInfo.SortIsDesc = false;
                }
                filterInfo.SortColumn = colDesc.Column;
                onFilterUpdate(filterInfo);
            }
        };
    }

    var createRefinementBlock = function (rOptions, i) {
        var rField = filterInfo.Refinements[i].Column;
        var rTitle = filterInfo.Refinements[i].Header;
        var rID = "Refinement-" + rField;
        var rType = "Refinement-" + (filterInfo.Refinements[i].Type.toLowerCase());
        var rBlock = $("<div>").addClass("InputBlock Refinement FullWidth " + rType).attr("data-field", rField).append(
            $("<label>").addClass("InputBlockHeader").attr("for", rID).text(rTitle)
        );

        switch (filterInfo.Refinements[i].Type) {
            case "Date":
                {
                    var rdAll = rID + "-all";
                    var rdDay = rID + "-day";
                    var rdRng = rID + "-range";

                    var IsInitializing = true;

                    var onDateTypeChange = function (e, IsInitializing) {
                        rOptions[i].SelectedIndex = $(this).attr("optIndex");
                        switch (this.value) {
                            case "all":
                                OptionDay.val("");
                                OptionRangeFrom.val("");
                                OptionRangeTo.val("");
                                if (IsInitializing) {
                                    OptionDay.hide();
                                    OptionRange.hide();
                                    IsInitializing = false;
                                }
                                else {
                                    OptionDay.slideUp(100);
                                    OptionRange.slideUp(100);
                                }
                                break;

                            case "day":
                                OptionRangeFrom.val("");
                                OptionRangeTo.val("");
                                if (IsInitializing) {
                                    OptionRange.hide();
                                    OptionDay.show();
                                    IsInitializing = false;
                                }
                                else {
                                    OptionRange.slideUp(100, function () {
                                        OptionDay.slideDown(100, function () {
                                            OptionDay.focus();
                                        });
                                    });
                                }
                                break;

                            case "range":
                                OptionDay.val("");
                                if (IsInitializing) {
                                    OptionDay.hide();
                                    OptionRange.show();
                                    IsInitializing = false;
                                }
                                else {
                                    OptionDay.slideUp(100, function () {
                                        OptionRange.slideDown(100, function () {
                                            OptionRangeFrom.focus();
                                        });
                                    });
                                }
                                break;
                        }
                    };

                    var OptionRangeFrom = $('<input type="text">').addClass('Refinement-date-from').datepicker({
                        dateFormat: 'M dd yy',
                        onSelect: function (selectedDate) {
                            rOptions[i].From = selectedDate;
                            OptionRangeTo.datepicker('option', 'minDate', selectedDate).focus(0);
                        }
                    });
                    var OptionRangeText = $('<span>').addClass('Refinement-date-separator').text('to');
                    var OptionRangeTo = $('<input type="text">').addClass('Refinement-date-to').datepicker({
                        dateFormat: 'M dd yy',
                        onSelect: function (selectedDate) {
                            rOptions[i].To = selectedDate;
                            OptionRangeFrom.datepicker('option', 'maxDate', selectedDate);
                        }
                    });

                    var OptionRange = $('<div>').addClass('Refinement-date-range').append(
                        OptionRangeFrom,
                        $('<span>').addClass('Refinement-date-separator').text('to'),
                        OptionRangeTo
                    );

                    var OptionDay = $('<input type="text">').addClass('Refinement-date-single').datepicker({
                        dateFormat: 'M dd yy',
                        onSelect: function (selectedDate) {
                            rOptions[i].From = selectedDate;
                        }
                    });

                    var DateRadios = $('<div>').addClass('Refinement-date-radios').append(
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdAll, "value": "all", "index": i, "optIndex": 0 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 0).change(onDateTypeChange),
                        $('<label>').attr('for', rdAll).text('All'),
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdDay, "value": "day", "index": i, "optIndex": 1 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 1).change(onDateTypeChange),
                        $('<label>').attr('for', rdDay).text('Single Day'),
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdRng, "value": "range", "index": i, "optIndex": 2 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 2).change(onDateTypeChange),
                        $('<label>').attr('for', rdRng).text('Date Range')
                    );

                    DateRadios.find('input:checked').trigger("change", "IsInitializing");

                    switch (filterInfo.Refinements[i].SelectedIndex) {
                        case 0:
                        default:
                            break;
                        case 1:
                            OptionDay.val(filterInfo.Refinements[i].From);
                            break;
                        case 2:
                            OptionRangeFrom.val(filterInfo.Refinements[i].From);
                            OptionRangeTo.val(filterInfo.Refinements[i].To);
                            break;
                    }

                    rBlock.append(DateRadios, OptionDay, OptionRange);
                }
                break;

            case "Number":
                {
                    var rdAll = rID + "-all";
                    var rdGt = rID + "-gt";
                    var rdLt = rID + "-lt";
                    var rdEq = rID + "-eq";
                    var rdRng = rID + "-range";

                    var IsInitializing = true;

                    var onDateTypeChange = function (e, IsInitializing) {
                        rOptions[i].SelectedIndex = $(this).attr("optIndex");
                        switch (this.value) {
                            case "all":
                                OptionSingle.val("0");
                                OptionRangeMin.val("0");
                                OptionRangeMax.val("0");
                                if (IsInitializing) {
                                    OptionSingle.hide();
                                    OptionRange.hide();
                                    IsInitializing = false;
                                }
                                else {
                                    OptionSingle.slideUp(100);
                                    OptionRange.slideUp(100);
                                }
                                break;

                            case "lt":
                            case "gt":
                            case "eq":
                                OptionRangeMin.val("0");
                                OptionRangeMax.val("0");
                                if (IsInitializing) {
                                    OptionRange.hide();
                                    OptionSingle.show();
                                    IsInitializing = false;
                                }
                                else {
                                    OptionRange.slideUp(100, function () {
                                        OptionSingle.slideDown(100, function () {
                                            OptionSingle.focus();
                                        });
                                    });
                                }
                                OptionSingle.val(rOptions[i].Min);
                                break;

                            case "range":
                                OptionSingle.val("0");
                                if (IsInitializing) {
                                    OptionSingle.hide();
                                    OptionRange.show();
                                    IsInitializing = false;
                                }
                                else {
                                    OptionSingle.slideUp(100, function () {
                                        OptionRange.slideDown(100, function () {
                                            OptionRangeMin.focus();
                                        });
                                    });
                                }
                                OptionRangeMin.val(rOptions[i].Min);
                                OptionRangeMax.val(rOptions[i].Max);
                                break;
                        }
                    };

                    var filterFunc = function (e) {
                        if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 110, 190]) !== -1 ||
                            (e.keyCode == 65 && e.ctrlKey === true) ||
                            (e.keyCode == 67 && e.ctrlKey === true) ||
                            (e.keyCode == 88 && e.ctrlKey === true) ||
                            (e.keyCode >= 35 && e.keyCode <= 39)) {
                            return;
                        }
                        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
                            e.preventDefault();
                        }
                    }

                    var OptionRangeMin = $('<input type="text">').addClass('Refinement-number-min').keydown(filterFunc).change(function () {
                        rOptions[i].Min = Number($(this).val()) || 0;
                        if (OptionRangeMax.val() < rOptions[i].Min) {
                            OptionRangeMax.val(rOptions[i].Min);
                        }
                    });
                    var OptionRangeMax = $('<input type="text">').addClass('Refinement-number-max').keydown(filterFunc).change(function () {
                        rOptions[i].Max = Number($(this).val()) || 0;
                        if (OptionRangeMin.val() > rOptions[i].Max) {
                            OptionRangeMin.val(rOptions[i].Max);
                        }
                    });

                    var OptionRange = $('<div>').addClass('Refinement-number-range').append(
                        OptionRangeMin,
                        $('<span>').addClass('Refinement-number-separator').text('to'),
                        OptionRangeMax
                    );

                    var OptionSingle = $('<input type="text">').addClass('Refinement-date-single').keydown(filterFunc).change(function () {
                        rOptions[i].Min = Number($(this).val()) || 0;
                    });

                    var DateRadios = $('<div>').addClass('Refinement-date-radios').append(
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdAll, "value": "all", "index": i, "optIndex": 0 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 0).change(onDateTypeChange),
                        $('<label>').attr('for', rdAll).text('All'),
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdGt, "value": "lt", "index": i, "optIndex": 1 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 1).change(onDateTypeChange),
                        $('<label>').attr('for', rdGt).text('Greater than'),
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdLt, "value": "gt", "index": i, "optIndex": 2 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 2).change(onDateTypeChange),
                        $('<label>').attr('for', rdLt).text('Less than'),
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdEq, "value": "eq", "index": i, "optIndex": 3 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 3).change(onDateTypeChange),
                        $('<label>').attr('for', rdEq).text('Equal To'),
                        $('<input>').attr({ "name": rID, "type": "radio", "id": rdRng, "value": "range", "index": i, "optIndex": 4 }).prop("checked", filterInfo.Refinements[i].SelectedIndex == 4).change(onDateTypeChange),
                        $('<label>').attr('for', rdRng).text('Between')
                    );

                    DateRadios.find('input:checked').trigger("change", "IsInitializing");

                    switch (filterInfo.Refinements[i].SelectedIndex) {
                        case 0:
                        default:
                            break;
                        case 1:
                        case 3:
                            OptionSingle.val(filterInfo.Refinements[i].Min);
                            break;
                        case 2:
                            OptionSingle.val(filterInfo.Refinements[i].Max);
                            break;
                        case 4:
                            OptionRangeMin.val(filterInfo.Refinements[i].Min);
                            OptionRangeMax.val(filterInfo.Refinements[i].Max);
                            break;
                    }

                    rBlock.append(DateRadios, OptionSingle, OptionRange);
                }
                break;

            case "Select":
            default:
                {
                    var rSelect = $("<select>").attr({ "name": rID, "id": rID }).addClass("GreySelect").change(function (e) {
                        rOptions[i].SelectedIndex = $(this)[0].selectedIndex;
                    });
                    var selected;
                    for (var opt = 0; opt < filterInfo.Refinements[i].SelectOptions.length; opt++) {
                        var option = $("<option>").text(filterInfo.Refinements[i].SelectOptions[opt]);
                        if (filterInfo.Refinements[i].SelectedIndex == opt) {
                            selected = option;
                        }
                        rSelect.append(
                            option
                        );
                    }
                    selected.prop("selected", true);
                    rBlock.append(rSelect);
                }
                break;
        }

        return rBlock;
    }

    var TableTitle = function () {
        if (!tableInfo.title) {
            return "";
        }

        if (!tableInfo.tooltip) {
            return $("<h2>").addClass("RedBottomHead DisplayTableHead").text(tableInfo.title);
        } else {
            return $("<div>").addClass("RedBottomHead DisplayTableHead").append(
                $("<h2>").addClass("tooltip-head").text(tableInfo.title),
                    $("<span>").addClass("tooltip-parent").append(
                        $("<img>").addClass("tooltip-link").attr({ "src": "images/question-mark.png", "width": 17, "height": 16, "alt": "?" }),
                        $("<div>").addClass("tooltip-content").append(
                            $("<img>").addClass("tooltip-arrow").attr({ "src": "images/tooltip-arrow.png", "width": 11, "height": 12, "alt": "arrow" }),
                            tableInfo.tooltip
                        )
                    ),
                    $("<div>").css("clear", "both")
                );
        }
    };

    var TableToolbar = function () {
        var hasToolbar = tableInfo.isToolbarEnabled;
        var jqToolbarFilter = $("<div>").addClass("toolbar-filter");
        if(tableInfo.isSearchEnabled)
        {
            hasToolbar = true;
            var jqSearchInput = $("<div>").addClass("search-input").append(
                $("<input>").addClass("BasicSearchQuery").attr({ "type": "text", "placeholder": "Enter Query" })
            ).keyup(function (e) {
                if (e.keyCode == 13) {
                    onBasicSearch();
                }
            });

            var searchlist = [];
            tableInfo.columns.forEach(function (col, index) {
                if (col.IsSearchable) {
                    searchlist.push(col);
                }
            });

            if (tableInfo.isAdvancedSearch && searchlist.length > 0) {
                var colOption = $("<select>").addClass("SearchField GreySelect").append(
                    $("<option>").attr("value", 0).text("Choose Column")
                );
                searchlist.forEach(function (col) {
                    colOption.append(
                        $("<option>").attr("value", col.Column).text(col.Header)
                    );
                });
                jqSearchInput.append(
                    $("<div>").addClass("advanced-search-trigger").text("▾").click(function () {
                        jqElemSelect(".advanced-search-panel").fadeToggle(250);
                    }),
                    $("<div>").addClass("advanced-search-panel").css("display", "none").append(
                        $("<img>").addClass("close-panel").attr({ "src": "images/x.png", "alt": "close", "width": 12, "height": 10 }).click(function () {
                            jqElemSelect(".SearchQuery").val("");
                            jqElemSelect(".advanced-search-panel").fadeOut(250);
                        }),
                        $("<p>").addClass("RedBottomHead").text("Advanced Search"),
                        $("<input>").addClass("SearchQuery").attr({ "type": "text", "placeholder": "Enter Query" }),
                        colOption,
                        $("<p>").css({ "color": "red", "display": "none" }).addClass("errColSelect").text("Select a column to search."),
                        $("<div>").addClass("button btnSm").css("cursor", "pointer").text("Search").click(function () {
                            var sIndex = jqElemSelect(".SearchField")[0].selectedIndex;
                            if (sIndex == 0) {
                                jqElemSelect(".errColSelect").show();
                            }
                            else {
                                jqElemSelect(".errColSelect").hide();
                                onAdvancedSearch();
                            }
                        })
                    )
                );
            }

            jqToolbarFilter.append(
                jqSearchInput,
                $("<div>").addClass("search-button filter-button").attr("title", "search").append(
                    $("<img>").attr("src", "images/table-search.png"),
                    $("<label>").text("Search")
                ).click(onBasicSearch)
            );

            if (tableInfo.isRefine) {
                jqToolbarFilter.append(
                $("<div>").addClass("refine-button filter-button").attr({ "title": "refine", "data-refine-options": tableInfo.refineOptions }).append(
                    $("<img>").attr("src", "images/table-refine.png"),
                    $("<label>").text("Refine")
                ).click(function () {
                    var rfHeader = $("<div>").append($("<h2>").addClass("RedBottomHead").text("Refine Results"));

                    var rfContent = $("<div>");
                    var rOptions = [];

                    for (var i = 0; i < filterInfo.Refinements.length; i++) {
                        rOptions.push({
                            "SelectedIndex": 0,
                            "From": "",
                            "To": "",
                            "Min": "0",
                            "Max": "0"
                        });
                        var rBlock = createRefinementBlock(rOptions, i);
                        rfContent.append(rBlock);
                    }

                    var rfFooter = $("<div>").addClass("FooterButtons").append(
                        $("<input>").attr("type", "button").addClass("button").val("Refine results").click(function () {
                            for (var i = 0; i < rOptions.length; i++) {
                                filterInfo.Refinements[i].SelectedIndex = rOptions[i].SelectedIndex;
                                filterInfo.Refinements[i].From = rOptions[i].From;
                                filterInfo.Refinements[i].To = rOptions[i].To;
                                filterInfo.Refinements[i].Min = rOptions[i].Min;
                                filterInfo.Refinements[i].Max = rOptions[i].Max;
                            }
                            tableInfo.onFilterUpdate(filterInfo);
                        }),
                        $("<input>").attr("type", "button").addClass("button CloseWindowBtn").val("Close")
                    );

                    var disp = new DisplayWindow({ "Size": "small", ContainerId: "WindowRefine" });

                    disp.Content.append(rfHeader, rfContent, rfFooter);

                    disp.show();
                }));
            }

            jqToolbarFilter.append(
            $("<div>").addClass("reset-button filter-button").attr("title", "reset").append(
                $("<img>").attr("src", "images/table-reset.png"),
                $("<label>").text("Reset").click(function () {
                    filterInfo.CurrentPage = 1;
                    filterInfo.SearchColumn = "";
                    filterInfo.SearchQuery = "";
                    filterInfo.SortColumn = "";
                    filterInfo.SortIsDesc = false;
                    for (var i = 0; i < filterInfo.Refinements.length; i++) {
                        filterInfo.Refinements[i].SelectedIndex = 0;
                    }
                    tableInfo.onFilterUpdate(filterInfo);
                })
            ));
        }

        var jqToolbarEdit = $("<div>").addClass("toolbar-edit");
        if (tableInfo.isCollection) {
            hasToolbar = true;
            jqToolbarEdit.append(
            $("<div>").append(
                $("<p>").addClass("ToolbarHead").text("\xA0"),
                $("<ul>").append(
                    $("<li>").addClass("CollCount tooltip").attr("data-tooltip", "Your current collection count").text("0")
                )
            )
        );
        }
        jqToolbarEdit.append(tableInfo.TableToolbarEditButtons());

        if(hasToolbar) {
            return $("<div>").addClass("toolbar clearfix").attr("data-rel-tbl", tableInfo.tableID).append(
                   jqToolbarEdit,
                   jqToolbarFilter
               );
        } else {
            return $("<div>");
        }
    };

    var TableCriteriaDetails = function () {
        if (!tableInfo.isCriteriaEnabled) {
            return "";
        }
        else {
            var searchResults = "";
            if (filterInfo.SearchQuery) {
                searchResults = $("<p>").addClass("SearchQuery").append(
                    "Showing all ",
                    $("<span>").addClass("Field").text("fields"),
                    " with: ",
                    $("<span>").addClass("Query").text(filterInfo.SearchQuery)
                )
            }

            var refineDesc = "";
            var refineContent = "";

            if (tableInfo.isRefine) {
                if (tableInfo.criteriaColumnCount < 1) {
                    tableInfo.criteriaColumnCount = 1;
                }
                else if (tableInfo.criteriaColumnCount > 5) {
                    tableInfo.criteriaColumnCount = 5;
                }

                var criteriaColumns = [];
                for (var i = 0; i < tableInfo.criteriaColumnCount; i++) {
                    criteriaColumns.push($("<div>").addClass("CriteriaDetailsColumn Count" + tableInfo.criteriaColumnCount));
                }

                for (var i = 0; i < filterInfo.Refinements.length; i++) {
                    criteriaColumns[i % criteriaColumns.length].append(
                        $("<div>").addClass("CriteriaDetailsItem").append(
                            $("<label>").text(filterInfo.Refinements[i].Header),
                            $("<output>").text(filterInfo.Refinements[i].SelectOptions[filterInfo.Refinements[i].SelectedIndex])
                        )
                    );
                }
                refineDesc = $("<p>").text("The table below displays records that meet the following criteria:");
                refineContent = $("<div>").addClass("CriteriaDetailsContent");

                for (var i = 0; i < criteriaColumns.length; i++) {
                    refineContent.append(criteriaColumns[i]);
                }
            }

            var criteriaPanel = $("<div>").addClass("CriteriaDetailsPanel").attr({ "data-default-is-open": tableInfo.isCriteriaDefaultOpen, "data-column-count": tableInfo.criteriaColumnCount }).append(
                $("<div>").addClass("CriteriaDetailsContentWrapper").append(
                    refineDesc,
                    refineContent,
                    searchResults,
                    $("<div>").addClass("clearfix")
                )
            );

            if (!tableInfo.isCriteriaDefaultOpen) {
                criteriaPanel.css("display", "none");
            }

            return $("<div>").addClass("CriteriaDetails clearfix").append(
                $("<div>").addClass("CriteriaDetailsButton").text("Criteria Details").css('cursor', 'pointer').click(function () {
                    var panel = jqElemSelect(".CriteriaDetailsPanel");
                    panel.slideToggle(100);
                }),
                $("<div>").addClass("clearfix"),
                criteriaPanel
            );
        }
    };

    var TableResultIndicator = function () {
        if (tableInfo.isPagination) {
            var resultCount = tableInfo.rows.length <= 0 ? 0 : (filterInfo.ResultCount === 0 ? tableInfo.rows.length : filterInfo.ResultCount);
            var s = (resultCount != 1) ? "s" : "";
            var noResMsg = resultCount === 0 ? $("<p>").addClass("NoResults").text("There are no items to show for your query.") : "";
            return $("<div>").addClass("ResultIndicator BtmMargin").append(
                noResMsg,
                $("<div>").addClass("ResultCount").append(
                    resultCount + " Total Result" + s,
                    $("<span>").addClass("JsHiddenResults").css("display", "none").append(
                        ", ",
                        $("<span>").addClass("HiddenCount").text("N"),
                        " Hidden"
                    )
                )
            );
        }
        return $("<div>");
    };

    var TableForm = function () {
        var trow = $("<tr>").css("text-align", "center");
        if (tableInfo.isCollection) {
            var checkBox = $("<input>").attr({ "type": "checkbox", "title": "Add all items to collection" }).addClass("RowSelectAll").click(onCollCheckAllClick);
            trow.append($("<th>").append(checkBox));
        }

        for (var i = 0; i < tableInfo.columns.length; i++) {
            var cont = $("<th>").attr({ "column": tableInfo.columns[i].Column, "scope": "col" });
            var lbl = $("<span>").addClass("Label").text(tableInfo.columns[i].Header).click(createColumnLabel(filterInfo, tableInfo.columns[i], tableInfo.onFilterUpdate));
            if (tableInfo.columns[i].IsSortable) {
                cont.css("cursor", "pointer").addClass("sortColumn");
                if (filterInfo.SortColumn && filterInfo.SortColumn == tableInfo.columns[i].Column) {
                    if (filterInfo.SortIsDesc) {
                        lbl.addClass("SortDesc");
                    }
                    else {
                        lbl.addClass("SortAsc");
                    }
                }
            }
            trow.append(cont.append(lbl));
        }

        var tbody = $("<tbody>");

        for (var i = 0; i < tableInfo.rows.length; i++) {
            var value = tableInfo.rows[i];
            var tr = $("<tr>").attr("rowindex", i);
            if (tableInfo.isCollection) {
                tr.append($("<td>").append($("<input>").attr({ "type": "checkbox", "title": "Add item to collection" }).addClass("RowSelect").click(onCollCheckClick)));
            }
            for (var colIndex = 0; colIndex < tableInfo.columns.length; colIndex++) {
                var colProp = tableInfo.columns[colIndex].Column;
                var colTD = $("<td>");
                if (colProp in tableInfo.replaces) {
                    colTD.append(tableInfo.replaces[colProp](i, value, isInit));
                }
                else {
                    colTD.append(defaultReplace(value[colProp]));
                }
                tr.append(colTD);
            };
            tbody.append(tr);
        }

        var pagination = $("<div>");
        if (tableInfo.isPagination) {
            if (tableInfo.rows.length > 0 && filterInfo.CurrentPage > 0 && filterInfo.MaxPage > 0) {

                var prevButton = $("<span>").attr("id", "prevPg").addClass("previous").text("Previous");
                var nextButton = $("<span>").addClass("next").attr("id", "nextPg").text("Next");

                if (filterInfo.CurrentPage == 1) {
                    prevButton.css("display", "none");
                }

                if (filterInfo.CurrentPage == filterInfo.MaxPage) {
                    nextButton.css("display", "none");
                }

                prevButton.click(function () {
                    filterInfo.CurrentPage--;
                    tableInfo.onFilterUpdate(filterInfo);
                });
                nextButton.click(function () {
                    filterInfo.CurrentPage++;
                    tableInfo.onFilterUpdate(filterInfo);
                });

                pagination.addClass("pagination").append(
                    $("<div>").css("text-align", "left").append(
                        prevButton
                    ),
                    $("<div>").addClass("PageIndicator").css({ "text-align": "center", "width": "32%" }).append(
                        "Page "
                    ).append($("<span>").addClass("currentPg").text(filterInfo.CurrentPage)).append(
                        " of "
                    ).append($("<span>").addClass("maxPg").text(filterInfo.MaxPage)),
                    $("<div>").css("text-align", "right").append(
                        nextButton
                    )
                );
            }
        }

        var tableSize = "TblScroll" + (tableInfo.isTallTable ? "Tall" : "Short");

        var ret = $("<div>").attr("id", tableInfo.tableID + "Form").addClass("TblForm").append(
            $("<table>").attr({ "id": tableInfo.tableID + "Header", "data-rel-tbl": tableInfo.tableID }).addClass("PaddedTbl TblHeader").append(
                $("<thead>").append(
                    trow
                )
            ),
            $("<div>").addClass("BtmMargin TblScroll " + tableSize).append(
                $("<table>").addClass("dataTable DataTbl PaddedTbl StripedTbl").attr("id", tableInfo.tableID).append(
                    tbody
                )
            ),
            pagination,
            TableResultIndicator(),
            tableInfo.TableBeforeBottomButtons(),
            $("<div>").addClass("TableBottomButtons").append(
                tableInfo.TableBottomButtons()
            )
        );
        return ret;
    };

    var TableNotes = function () {
        if (tableInfo.isNotesEnabled) {
            return $("<div>").addClass("notes-section").append(
                $("<p>").addClass("expand-handle").css("border", "none").text("Notes").click(function () {
                    if ($(this).hasClass('expand-handle-open')) {
                        $(this).next("div").slideUp();
                        $(this).removeClass("expand-handle-open");
                    }
                    else {
                        $(this).addClass("expand-handle-open");
                        $(this).next("div").slideDown();
                        if (onOpen) {
                            onOpen($(this).next("div"));
                        }
                    }
                }),
                $("<div>").addClass("expand-content").css("border", "none").append(tableInfo.TableNotesContents())
            );
        }
        else {
            return "";
        }
    };

    this.SetOptions(options);
}
