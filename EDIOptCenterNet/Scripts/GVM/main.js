(function () {
    var startDate = $("#startDate");
    var endDate = $("#endDate");

    $(function () {
        var reportButton = $("#reportButton");

        startDate.datepicker({
            dateFormat: "M dd yy"
        });
        endDate.datepicker({
            dateFormat: "M dd yy"
        });

        $("#reportButton").click(submitRequest);
    });

    function submitRequest() {
        // verify start date and end date
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
                EndDate: ed
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
            submitAJAXRequest("GVMCostReport.aspx/GenerateReport", request, goodFunc, failFunc);
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

} ());