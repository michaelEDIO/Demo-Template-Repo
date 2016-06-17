var MainTable = (function () {
    var RowModel = (function () {
        var buildModel = function (selector) {
            var dict = {};
            var rows = $(selector);
            for (var i = 0; i < rows.length; i++) {
                dict[$(".VendorID", rows[i]).text()] = rows[i];
            }
            return dict;
        }

        var kDict = buildModel(".mainTable > tbody > tr");
        var kBaseDict = buildModel(".prevTable > tbody > tr");
        var days = [];
        var minDollar = 0;
        var dayRange = 0;

        var getDays = function () {
            days = [
                $(".sunCheckbox input:checkbox").prop("checked"),
                $(".monCheckbox input:checkbox").prop("checked"),
                $(".tueCheckbox input:checkbox").prop("checked"),
                $(".wedCheckbox input:checkbox").prop("checked"),
                $(".thuCheckbox input:checkbox").prop("checked"),
                $(".friCheckbox input:checkbox").prop("checked"),
                $(".satCheckbox input:checkbox").prop("checked")
            ];
            minDollar = $("#MainContent_minDollarBox").val();
            dayRange = $("#MainContent_dayRangeBox").val();
        }
        var setDays = function () {
            $(".sunCheckbox input:checkbox").prop("checked", days[0]);
            $(".monCheckbox input:checkbox").prop("checked", days[1]);
            $(".tueCheckbox input:checkbox").prop("checked", days[2]);
            $(".wedCheckbox input:checkbox").prop("checked", days[3]);
            $(".thuCheckbox input:checkbox").prop("checked", days[4]);
            $(".friCheckbox input:checkbox").prop("checked", days[5]);
            $(".satCheckbox input:checkbox").prop("checked", days[6]);
            $("#MainContent_minDollarBox").val(minDollar);
            $("#MainContent_dayRangeBox").val(dayRange);
        }

        var setValueOfCurBox = function (jqCurrent, setVal, def, isLabel) {
            if (setVal) {
                if (isLabel) {
                    if ($.isNumeric(setVal)) {
                        jqCurrent.text(setVal);
                    }
                    else if (setVal.toLowerCase() == "d") {
                        jqCurrent.text(def);
                    }
                }
                else {
                    if ($.isNumeric(setVal)) {
                        jqCurrent.val(setVal);
                    }
                    else if (setVal.toLowerCase() == "d") {
                        jqCurrent.val(def);
                    }
                }
            }
        }

        var set = function (useBase, key, val) {
            var dictRef = kDict;
            if (useBase) {
                dictRef = kBaseDict;
            }
            if (key in dictRef) {
                setValueOfCurBox($(".CurMin", dictRef[key]), val.Min, $(".BaseMin", dictRef[key]).text(), useBase);
                setValueOfCurBox($(".CurMax", dictRef[key]), val.Max, $(".BaseMax", dictRef[key]).text(), useBase);
                setValueOfCurBox($(".CurReorder", dictRef[key]), val.Reorder, $(".BaseReorder", dictRef[key]).text(), useBase);
                return true;
            }
            else {
                return false;
            }
        }

        var sync = function () {
            var keys = Object.keys(kDict);
            for (var i = 0; i < keys.length; i++) {
                set(true, keys[i], {
                    Min: $(".CurMin", kDict[keys[i]]).val(),
                    Max: $(".CurMax", kDict[keys[i]]).val(),
                    Reorder: $(".CurReorder", kDict[keys[i]]).val()
                });
            }

            var allSet = days[0] && days[1] && days[2] && days[3] && days[4] && days[5] && days[6];
            var allUnset = !(days[0] || days[1] || days[2] || days[3] || days[4] || days[5] || days[6]);
            var daysText = "";
            if (allSet) {
                daysText = "Every Day";
            }
            else if (allUnset) {
                daysText = "No Days Selected";
            }
            else {
                if (days[0]) {
                    daysText += "Sunday";
                }
                if (days[1]) {
                    if (daysText.length > 0) {
                        daysText += ", ";
                    }
                    daysText += "Monday";
                }
                if (days[2]) {
                    if (daysText.length > 0) {
                        daysText += ", ";
                    }
                    daysText += "Tuesday";
                }
                if (days[3]) {
                    if (daysText.length > 0) {
                        daysText += ", ";
                    }
                    daysText += "Wednesday";
                }
                if (days[4]) {
                    if (daysText.length > 0) {
                        daysText += ", ";
                    }
                    daysText += "Thursday";
                }
                if (days[5]) {
                    if (daysText.length > 0) {
                        daysText += ", ";
                    }
                    daysText += "Friday";
                }
                if (days[6]) {
                    if (daysText.length > 0) {
                        daysText += ", ";
                    }
                    daysText += "Saturday";
                }
            }
            $(".daysText").text(daysText);
            $("#MainContent_minDollarLabel").text(minDollar);
            $("#MainContent_dayRangeLabel").text(dayRange);
        }

        return {
            RefreshBase: function () {
                getDays();
                sync();
            },
            RefreshEdit: function () {
                setDays();
            },
            SetBase: function (key, val) {
                return set(true, key, val);
            },
            Set: function (key, val) {
                return set(false, key, val);
            },
            SetChecked: function (val) {
                var keys = this.Keys();
                for (var i = 0; i < keys.length; i++) {
                    if (this.IsChecked(keys[i])) {
                        this.Set(keys[i], val);
                    }
                }
            },
            Get: function (key) {
                if (key in kDict) {
                    return {
                        Min: $(".CurMin", kDict[key]).val(),
                        Max: $(".CurMax", kDict[key]).val(),
                        Reorder: $(".CurReorder", kDict[key]).val()
                    };
                }
                else {
                    return undefined;
                }
            },
            GetChecked: function () {
                var keys = this.Keys();
                var ret = {};
                for (var i = 0; i < keys.length; i++) {
                    if (this.IsChecked(keys[i])) {
                        ret[keys[i]] = this.Get(keys[i]);
                    }
                }
                return ret;
            },
            GetAll: function () {
                var keys = this.Keys();
                var ret = {};
                for (var i = 0; i < keys.length; i++) {
                    ret[keys[i]] = this.Get(keys[i]);
                }
                return ret;
            },
            IsChecked: function (key) {
                if (key in kDict) {
                    return $("input", kDict[key]).prop("checked");
                }
                else {
                    return undefined;
                }
            },
            Keys: function () {
                return Object.keys(kDict);
            }
        };
    } ());

    var initialData = {};

    return {
        RefreshBase: function () {
            RowModel.RefreshBase();
        },
        RefreshEdit: function () {
            RowModel.RefreshEdit();
        },
        SetCurrentDataAsDefault: function () {
            var rowKeys = RowModel.Keys();
            for (var i = 0; i < rowKeys.length; i++) {
                initialData[rowKeys[i]] = RowModel.Get(rowKeys[i]);
            }
        },
        GetUpdateData: function () {
            var newData = RowModel.GetAll();
            // Now, compare data by keys...
            var ret = {};
            for (var key in newData) {
                // Compare key by key.
                if (key in initialData &&
                    initialData[key].Min != newData[key].Min ||
                    initialData[key].Max != newData[key].Max ||
                    initialData[key].Reorder != newData[key].Reorder) {
                    // If data diffs, then add to update data.
                    ret[key] = newData[key];
                }
            }
            // ret now has the diff rows.
            var pack = {
                days: [false, false, false, false, false, false, false],
                minDollars: 0,
                dayRange: 0,
                itemInfo: ret
            };

            pack.days[0] = $(".sunCheckbox input:checkbox").prop("checked");
            pack.days[1] = $(".monCheckbox input:checkbox").prop("checked");
            pack.days[2] = $(".tueCheckbox input:checkbox").prop("checked");
            pack.days[3] = $(".wedCheckbox input:checkbox").prop("checked");
            pack.days[4] = $(".thuCheckbox input:checkbox").prop("checked");
            pack.days[5] = $(".friCheckbox input:checkbox").prop("checked");
            pack.days[6] = $(".satCheckbox input:checkbox").prop("checked");
            pack.minDollars = $("#MainContent_minDollarBox").val();
            pack.dayRange = $("#MainContent_dayRangeBox").val();

            return pack;
        },
        SetChecked: function (val) {
            RowModel.SetChecked(val);
        },
        ClearChecked: function () {
            $("input:checked").prop('checked', false);
        }
    };
} ());

(function () {
    $(function () {
        applyStyling();
        attachListeners();
        MainTable.SetCurrentDataAsDefault();
        MainTable.RefreshBase();
    });

    function applyStyling() {
        // Header
        //$(".idmHeader").css('height', $(".tooltip-head").css('height'));

        // Table
        var rows = $(".dataTable > tbody > tr");
        rows.filter(":odd").addClass("TblRowLight");
        rows.filter(":even").addClass("TblRowDark");
    }

    function attachListeners() {
        // Filter apply div to ([dD]|[0-9]+)
        $(".applyBox").keydown(function (event) {
            event = (event) ? event : window.event;
            var charCode = (event.which) ? event.which : event.keyCode;
            if (charCode <= 31) {
                // Control
                return true;
            }
            else if (charCode >= 48 && charCode <= 57) {
                // [0-9]
                return true;
            }
            else if (charCode == 68) {
                // [dD]
                return true;
            }
            else {
                return false;
            }
        });

        // Apply vals to min/max/reorder boxes
        $("#applyButton").click(function () {
            var apply = {
                Min: $(".minApplyBox").val(),
                Max: $(".maxApplyBox").val(),
                Reorder: $(".reoApplyBox").val()
            };
            MainTable.SetChecked(apply);
        });

        // Filter number boxes to [0-9]+
        $(".numBox").keydown(function (event) {
            event = (event) ? event : window.event;
            var charCode = (event.which) ? event.which : event.keyCode;
            if (charCode <= 31) {
                // Control
                return true;
            }
            else if (charCode >= 48 && charCode <= 57) {
                // [0-9]
                return true;
            }
            else {
                return false;
            }
        });

        var updating = false;

        // Send changes to server
        $("#commitButton").click(function () {
            if (!updating) {
                updating = true;
                var setResp = function (msg) {
                    MainTable.RefreshBase();
                    Working.Hide();
                    updating = false;
                    MainTable.ClearChecked();
                    MainTable.SetCurrentDataAsDefault();
                    $("#closeButton").click();
                };
                var fail = function () { updating = false; defaultFailureFunc(); } //show err msg
                Working.Show();

                submitAJAXRequest("IDM.aspx/UpdateData", MainTable.GetUpdateData(), setResp, fail);
            }
        });

        // Open edit window
        $('.ToolbarEditHeadBtn').click(function () {
            MainTable.RefreshEdit();
            var dWindow = new DisplayWindow({ Size: 'large' }); //create new instance
            dWindow.Content.html($("#editField").show());
            dWindow.BeforeCloseCallbacks.push(function () {
                $("#mainField").after($("#editField").hide());
            });
            dWindow.show();
        });
    }
} ());
