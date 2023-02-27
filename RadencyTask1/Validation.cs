using System;

namespace RadencyTask1
{
    public interface IValidationStrategy<T> where T : InputData
    {
        bool validate(T data);
    }
    class PaymentValidation : IValidationStrategy<PaymentData>
    {
        public bool validate(PaymentData record)
        {
            if (string.IsNullOrEmpty(record.FirstName))
                return false;
            if (string.IsNullOrEmpty(record.LastName))
                return false;
            if (string.IsNullOrEmpty(record.Address))
                return false;
            if (string.IsNullOrEmpty(record.City))
                return false;
            if (record.Payment < 0)
                return false;
            if (record.Date > DateTime.Now)
                return false;
            if (record.AccountNumber <= 0)
                return false;
            if (string.IsNullOrEmpty(record.Service))
                return false;
            return true;
        }
    }

    class PotentialOtherClassValidation : IValidationStrategy<OtherData> 
    {
        public bool validate(OtherData data)
        {
            return true;
        }
    }
    /*
    class ValidationContext<T> where T : InputData
    {
        private readonly IValidationStrategy<T> validationStrategy;

        public ValidationContext(IValidationStrategy<T> strategy)
        {
            validationStrategy = strategy;
        }

        public bool validate(T data)
        {
            return validationStrategy.validate(data);
        }
    }
    */
    
}
