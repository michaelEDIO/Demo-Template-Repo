function OnUpdateValidators()
{
   for (var i = 0; i < Page_Validators.length; i++)
   {
      var val = Page_Validators[i];
      var ctrl = document.getElementById(val.controltovalidate);
      if (ctrl != null && ctrl.style != null)
      {
          if (!val.isvalid) {
              ctrl.style.background = '#AD2D2D';
          }
          else {
              ctrl.style.backgroundColor = '';
          }
      }
   }
}
 
function NewValidatorUpdate(val) { 
    OldValidatorUpdateDisplay(val);

    if (val.style.display === "inline" || val.style.visibility === "visible")
        val.style.display = "block";
}

OldValidatorUpdateDisplay = ValidatorUpdateDisplay;
ValidatorUpdateDisplay = NewValidatorUpdate;

$(".VerticalForm td").css("padding-bottom", "0");
