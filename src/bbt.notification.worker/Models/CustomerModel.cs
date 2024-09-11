namespace bbt.notification.worker.Models
{
    public class CustomerBaseRequestModel
    {
        public CustomerBaseRequestModel()
        {
            Page = 1;

            Size = 1;
        }

        public int Page { get; set; }

        public int Size { get; set; }
    }


    public class CustomerSearchRequestModel : CustomerBaseRequestModel
    {
        public CustomerSearchRequestModel() : base()
        {
        }

        public string? Name { get; set; }
    }

    public class CustomerRequestModel : CustomerBaseRequestModel
    {
        public CustomerRequestModel() : base()
        {
        }

        public CustomerName? Name { get; set; }
        public string? IdentityNumber { get; set; }
        public long? CustomerNumber { get; set; }
    }

    public class CustomerResponseModel
    {
        public int RecordCount { get; set; }
        public int ReturnCode { get; set; }
        public string? ReturnDescription { get; set; }
        public List<CustomerModel>? CustomerList { get; set; }
    }

    public class CustomerModel
    {
        public long CustomerNumber { get; set; }
        public CustomerName? Name { get; set; }
        public string? CitizenshipNumber { get; set; }
        public string? TaxNo { get; set; }
        public bool IsStaff { get; set; }
        public GsmPhone? GsmPhone { get; set; }
        public string? Email { get; set; }
        public string? BusinessEmail { get; set; }
        public string? BusinessLine { get; set; }
        //public Device? Device { get; set; }
        public long BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? PassportNo { get; set; }
        public string? RecordStatus { get; set; }
        public CustomerAddress? CustomerAddress { get; set; }

        public string? IdentityNumber
        {
            get
            {
                if (string.IsNullOrEmpty(CitizenshipNumber))
                {
                    return TaxNo;
                }

                return CitizenshipNumber;
            }
        }
    }

    public class GsmPhone
    {
        public string? Country { get; set; }
        public string? Prefix { get; set; }
        public string? Number { get; set; }
    }

    public class CustomerName
    {
        public string? First { get; set; }
        public string? Last { get; set; }
    }

    public class Device
    {
        public string? Label { get; set; }
        public string? DeviceId { get; set; }
    }

    public class CustomerAddress
    {
        public string District { get; set; } = default!;
        public string Street { get; set; } = default!;
        public string AddressDetail { get; set; } = default!;
        public string Town { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Address { get; set; } = default!;
    }
}