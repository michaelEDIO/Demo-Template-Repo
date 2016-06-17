using System.Collections.Generic;

namespace EDIOptions.AppCenter
{
    public static class ResponseDescription
    {
        private static Dictionary<ResponseType, string> ErrorToDetail = new Dictionary<ResponseType, string>()
        {
            {ResponseType.ErrorUnknown, "An unknown error occurred."},
            {ResponseType.ErrorAuth, "Access is denied."},
            {ResponseType.ErrorInvalidData, "The data request was not properly formatted."},
            {ResponseType.ErrorSupportEmailRequired, "A support request requires an email address."},
            {ResponseType.ErrorSupportMessageRequired, "A support request requires a message."},
            {ResponseType.ErrorSupportUnknownError, "The support request could not be entered. Please try again later."},
            {ResponseType.ErrorSalesReqUnknown, "An unknwon error occurred."},
            {ResponseType.ErrorIDMUnknown, "An unknown error occurred while trying to update inventory info. Please contact EDI Options."},
            {ResponseType.ErrorCPOUnknown, "An unknown error occurred while trying to apply the PO change. Please contact EDI Options."},
            {ResponseType.ErrorCPOPurposeUnrecognized, "The purpose in the PO header could not be recognized. Please contact EDI Options."},
            {ResponseType.ErrorCPOChangeUnrecognized, "One or more of the change types in the PO change could not be recognized. Please contact EDI Options."},
            {ResponseType.ErrorCPOMissingPrice,  "One or more line item changes in this PO change could not be applied. Please contact EDI Options."},
            {ResponseType.ErrorCPOMissingQuantity,  "One or more line item changes in this PO change could not be applied. Please contact EDI Options."},
            {ResponseType.ErrorCPOCouldNotApplyItemChange, "One or more line item changes in this PO change could not be applied. Please contact EDI Options."},
            {ResponseType.ErrorGVMUnknown, "An unknown error occurred while trying to generate the report. Please contact EDI Options."},
            {ResponseType.ErrorGVMDateRangeInvalid, "The date range sent was invalid. Please try again."},
            {ResponseType.ErrorGVMReportGenFailed, "The report could not be generated because of an error. Please contact EDI Options."},
            {ResponseType.ErrorAPOUnknown, "An unknown error occurred while uploading file for verification. Please contact EDI Options."},
            {ResponseType.ErrorAPOInvalidPO,"The PO number is missing or invalid. Check and try again."},
            {ResponseType.ErrorAPOInvalidStatusCode,"The status code of the sheet or one of the line items is missing or invalid. Check and try again."},
            {ResponseType.ErrorAPOInvalidShipDate,"The expected ship date of one of the line items is invalid. Check and try again."},
            {ResponseType.ErrorAPOInvalidDelivDate,"The delivery date of one of the line items is invalid. Check and try again."},
            {ResponseType.ErrorAPOInvalidNewQty,"The new quantity field of of the line items is invalid. Check and try again."},
            {ResponseType.ErrorAPOInvalidLineCount,"The line count is invalid. Check and try again."},
            {ResponseType.ErrorAPOInvalidStatusDesc,"The status description of one of the line items is invalid. Check and try again."},
            {ResponseType.ErrorLOCDateRangeInvalid, "The date range sent is invalid. Please try again."},
            {ResponseType.ErrorLOCReportGenFailed, "The report could not be generated because of an error. Please contact EDI Options."},
            {ResponseType.ErrorLOCUnknown, "An unknown error occurred while trying to generate the report. Please contact EDI Options."},
            {ResponseType.ErrorGeneralNoPartnerIndex, "No partner index specified."},
            {ResponseType.ErrorGeneralInvalidPartnerIndex, "An invalid partner index was specified."},
            {ResponseType.ErrorGeneralPartnerIndexOutOfRange, "Partner index was out of range."},
            {ResponseType.ErrorUploadFileTooLarge, "The file you uploaded was over the file limit (2 MB). Please try again."},
            {ResponseType.ErrorUploadFileFormatNotSupported, "The type of file you tried to upload is not supported. Plese try again."},
            {ResponseType.ErrorUploadUnknown, "An unknown error occurred while trying to upload your file. Please try again."},
            {ResponseType.ErrorITMInvoiceSendUnknown, "One or more of your invoices could not be sent."},
            {ResponseType.WarningAPOUnverifiedAccept, "Acknowledgment uploaded without verification. If this behavior is not desired, please contact EDI Options."},
            {ResponseType.SuccessSupportRequest, "Your report has been submitted successfully."},
            {ResponseType.SuccessCPO, "PO change applied successfully."},
            {ResponseType.SuccessAPO, "Acknowledgement successfully uploaded."}
        };

        public static string Get(ResponseType errType)
        {
            if (ErrorToDetail.ContainsKey(errType))
            {
                return ErrorToDetail[errType];
            }
            else
            {
                return "An unknown error occurred.";
            }
        }
    }
}