namespace PSAAS.Domain
{
    public class Tenant
    {
        private Tenant()
        {
            Id = Guid.Empty;
            CompanyName = string.Empty;
            VatNumber = string.Empty;
            CompleteVatNumber = string.Empty;
            CertifiedEmail = string.Empty;
            Address = new Address(string.Empty, string.Empty, string.Empty, string.Empty);
        }

        private Tenant(string companyName, string vatNumber, string completeVatNumber, string certifiedEmail,
            Address address)
        {
            Id = Guid.NewGuid();
            CompanyName = companyName;
            VatNumber = vatNumber;
            CompleteVatNumber = completeVatNumber;
            CertifiedEmail = certifiedEmail;
            Address = address;
        }

        public Guid Id { get; }
        public string CompanyName { get; }
        public string VatNumber { get; }
        public string CompleteVatNumber { get; }
        public string CertifiedEmail { get; }
        public Address Address { get; }

        public static Tenant CreateEmpty()
        {
            return new Tenant();
        }

        public static Tenant Create(string companyName, string vatNumber, string completeVatNumber,
            string certifiedEmail, Address address)
        {
            return new Tenant(companyName, vatNumber, completeVatNumber, certifiedEmail, address);
        }

        public bool IsValidVat()
        {
            return !string.IsNullOrEmpty(VatNumber) && VatNumber.Length == 11 && VatNumber.All(char.IsDigit);
        }

        public bool IsValidCompleteVat()
        {
            return !string.IsNullOrEmpty(CompleteVatNumber) && CompleteVatNumber.Length == 13;
        }

        public bool Equals(Tenant other)
        {
            if (other == null) return false;
            return Id.Equals(other.Id) && CompanyName.Equals(other.CompanyName) && VatNumber.Equals(other.VatNumber) &&
                   CompleteVatNumber.Equals(other.CompleteVatNumber) && CertifiedEmail.Equals(other.CertifiedEmail) &&
                   Address.Equals(other.Address);
        }
    }
}