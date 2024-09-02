using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EGS.Helpers
{
    public class GuidValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return new ValidationResult("Field is required.");
            }

            string guidPattern = @"^[{(]?[0-9a-fA-F]{8}[-]?[0-9a-fA-F]{4}[-]?[4][0-9a-fA-F]{3}[-]?[89abAB][0-9a-fA-F]{3}[-]?[0-9a-fA-F]{12}[)}]?$";
            bool isValidGuid = Regex.IsMatch(value.ToString(), guidPattern);

            if (!isValidGuid)
            {
                return new ValidationResult("Field must be a valid GUID.");
            }

            return ValidationResult.Success;
        }
    }
}