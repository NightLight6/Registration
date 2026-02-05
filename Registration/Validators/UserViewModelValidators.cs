using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Registration.Model;

namespace Registration.UserViewModelValidators
{
    public class UserViewModelValidator
    {
        public List<ValidationResult> Validate(UserViewModel model)
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }
    }
}