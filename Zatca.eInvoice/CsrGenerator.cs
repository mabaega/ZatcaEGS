using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;

namespace Zatca.eInvoice
{
    public class CsrGenerator
    {
        private readonly X509CertificateGenerator x509CertificateGenerator = new X509CertificateGenerator();

        public (string csr, string privateKey, List<string> errorMessages) GenerateCsrAndPrivateKeyFromConfig(string configFile, EnvironmentType environment, bool pemFormat = false)
        {
            try
            {
                CsrGenerationDto csrGenerationDto = ReadCsrConfig(configFile);

                var (csr, privateKey, errorMessages) = GenerateCsrAndPrivateKey(csrGenerationDto, environment, pemFormat);

                return (csr, privateKey, errorMessages);
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating CSR and private key from config", ex);
            }
        }

        public (string csr, string privateKey, List<string> errorMessages) GenerateCsrAndPrivateKey(CsrGenerationDto csrGenerationDto, EnvironmentType environment, bool pemFormat = false)
        {

            List<string> errorMessages = new List<string>();

            if (!csrGenerationDto.IsValid(out errorMessages))
            {
                throw new Exception("CSR configuration is not valid. Errors: " + string.Join(", ", errorMessages));
            }

            AsymmetricCipherKeyPair keyPair = GenerateKeyPair();
            string csr = GenerateCertificate(csrGenerationDto, keyPair, environment, pemFormat);

            string privateKey = GeneratePrivateKey(keyPair, pemFormat);

            return (csr, privateKey, errorMessages);
        }

        private CsrGenerationDto ReadCsrConfig(string configFile)
        {
            var lines = File.ReadAllLines(configFile);
            var config = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && line.Contains('='))
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        config[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            return new CsrGenerationDto
            {
                CommonName = GetValueOrDefault(config, "csr.common.name"),
                SerialNumber = GetValueOrDefault(config, "csr.serial.number"),
                OrganizationIdentifier = GetValueOrDefault(config, "csr.organization.identifier"),
                OrganizationUnitName = GetValueOrDefault(config, "csr.organization.unit.name"),
                OrganizationName = GetValueOrDefault(config, "csr.organization.name"),
                CountryName = GetValueOrDefault(config, "csr.country.name"),
                InvoiceType = GetValueOrDefault(config, "csr.invoice.type"),
                LocationAddress = GetValueOrDefault(config, "csr.location.address"),
                IndustryBusinessCategory = GetValueOrDefault(config, "csr.industry.business.category")
            };
        }

        private string GetValueOrDefault(Dictionary<string, string> config, string key)
        {
            return config.TryGetValue(key, out string value) ? value : null;
        }
        private string GenerateCertificate(CsrGenerationDto dto, AsymmetricCipherKeyPair keyPair, EnvironmentType environment, bool pemFormat = false)
        {
            try
            {
                return x509CertificateGenerator.CreateCertificate(dto, keyPair, pemFormat, environment);
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating CSR String", ex);
            }
        }

        private AsymmetricCipherKeyPair GenerateKeyPair()
        {
            ECKeyGenerationParameters parameters = new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256k1, new SecureRandom());
            ECKeyPairGenerator generator = new ECKeyPairGenerator();
            generator.Init(parameters);
            return generator.GenerateKeyPair();
        }

        private string GeneratePrivateKey(AsymmetricCipherKeyPair keyPair, bool pemFormat)
        {
            try
            {
                return GetPrivateKey(keyPair, pemFormat);
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating EC Private Key String", ex);
            }
        }

        private string GetPrivateKey(AsymmetricCipherKeyPair keys, bool pemFormat)
        {
            StringWriter stringWriter = new StringWriter();
            PemWriter pemWriter = new PemWriter(stringWriter);
            pemWriter.WriteObject(keys.Private);
            pemWriter.Writer.Flush();

            string privateKeyString = stringWriter.ToString();

            if (!pemFormat)
            {
                privateKeyString = privateKeyString
                    .Replace("-----BEGIN EC PRIVATE KEY-----", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace("-----END EC PRIVATE KEY-----", "");
            }

            return privateKeyString;
        }
    }
}