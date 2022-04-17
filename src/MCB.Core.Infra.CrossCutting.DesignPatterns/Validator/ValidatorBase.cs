using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Validator;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Validator.Enums;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Validator.Models;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Validator
{
    public abstract class ValidatorBase<T>
        : IValidator<T>
    {
        // Fields
        private readonly FluentValidationValidatorWrapper _fluentValidationValidatorWrapper;

        // Constructors
        protected ValidatorBase()
        {
            _fluentValidationValidatorWrapper = new FluentValidationValidatorWrapper();
            ConfigureFluentValidationConcreteValidator(_fluentValidationValidatorWrapper);
        }

        // Private Methods
        private static ValidationMessageType CreateValidationMessageType(FluentValidation.Severity severity)
        {
            ValidationMessageType validationMessageType;

            if (severity == FluentValidation.Severity.Error)
                validationMessageType = ValidationMessageType.Error;
            else if(severity == FluentValidation.Severity.Warning)
                validationMessageType = ValidationMessageType.Warning;
            else
                validationMessageType = ValidationMessageType.Information;

            return validationMessageType;
        }
        private static ValidationResult CreateValidationResult(FluentValidation.Results.ValidationResult fluentValidationValidationResult)
        {
            var validationMessageCollection = new List<ValidationMessage>();

            foreach (var validationFailure in fluentValidationValidationResult.Errors)
                validationMessageCollection.Add(
                    new ValidationMessage(
                        validationMessageType: CreateValidationMessageType(validationFailure.Severity),
                        code: validationFailure.ErrorCode,
                        description: validationFailure.ErrorMessage
                    )
                );

            return new ValidationResult(validationMessageCollection);
        }

        // Protected Methods
        protected abstract void ConfigureFluentValidationConcreteValidator(FluentValidationValidatorWrapper fluentValidationValidatorWrapper);

        // Public Methods
        public ValidationResult Validate(T instance)
        {
            return CreateValidationResult(_fluentValidationValidatorWrapper.Validate(instance));
        }
        public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken)
        {
            return CreateValidationResult(await _fluentValidationValidatorWrapper.ValidateAsync(instance, cancellationToken));
        }

        #region Fluent Validation Wrapper
        public class FluentValidationValidatorWrapper
            : FluentValidation.AbstractValidator<T>
        {

        } 
        #endregion
    }
}
