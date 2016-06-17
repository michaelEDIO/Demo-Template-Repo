(function () {
    function updatePastRecordList() {
        submitAJAXRequest("POAcknowledge.aspx/GetPastRequests", "", function (response) {
            var resp = JSON.parse(response.d);
            if (resp.success && resp.data.length > 0) {
                $("#noRecordDiv").hide();
                var recordList = resp.data;
                var headList = [
                    "Date",
                    "Type",
                    "Status"
                ];
                $("#pastRecordDiv").empty();
                $("#pastRecordDiv").append(createDataTable("pastRecords", headList.length, recordList.length, function (index) {
                    return $("<th>").prop("scope", "col").text(headList[index]);
                }, function (index) {
                    var statusTD = $("<td>");
                    var statusIcon = $("<span>").addClass("SmBtmMargin")
                    statusTD.append(statusIcon);
                    if (recordList[index].Status == "Y") {
                        statusIcon.addClass("good");
                        statusTD.append("Processed");
                    }
                    else if (recordList[index].Status == "X") {
                        statusIcon.addClass("warning");
                        statusTD.append("Error");
                    }
                    else {
                        statusIcon.addClass("neutral");
                        statusTD.append("Pending");
                    }

                    return $("<tr>").attr("rowindex", index).append(
                    $("<td>").text(recordList[index].Date),
                    $("<td>").text(recordList[index].Type),
                    statusTD
                );
                }));
                applyTableStyling();
                applyTableHeaderStyle($("#pastRecordDiv"));
            }
            else {
                $("#noRecordDiv").show();
            }
        });
    }

    function getTemplateLink() {
        submitAJAXRequest("POAcknowledge.aspx/GetTemplateLink", "", function (response) {
            var resp = JSON.parse(response.d);
            if (resp.success && resp.data !== "") {
                $("#templateLink").attr("href", resp.data).show();
                $("#noTemplateDiv").hide();
            }
        });
    }

    $(function () {
        updatePastRecordList();
        getTemplateLink();
        AddUploadControl($("#upFrame"), function () {
            var checkUpFunc = function (response) {
                var resp = JSON.parse(response.d);
                switch (resp.type) {
                    case "good":
                    default:
                        {
                            if (resp.data) {
                                ShowSmallAlertWindow("Upload Success", resp.type, resp.data);
                            }
                        }
                        break;
                    case "caution":
                        ShowSmallAlertWindow("Upload Warning", resp.type, resp.data);
                        break;
                    case "warning":
                        ShowSmallAlertWindow("Upload Error", resp.type, resp.data.msg);
                        break;
                }
                updatePastRecordList();
            };
            submitAJAXRequest("POAcknowledge.aspx/CheckUpload", "", checkUpFunc);
        });
    });
} ());

