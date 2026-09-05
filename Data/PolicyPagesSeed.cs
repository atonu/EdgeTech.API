using EdgeTech.API.Models;
using MongoDB.Driver;

namespace EdgeTech.API.Data;

public static class PolicyPagesSeed
{
    public static List<PolicyPage> GetDefaultPages()
    {
        return new List<PolicyPage>
        {
            // 1. Terms and Conditions
            new PolicyPage
            {
                Slug = "terms",
                Title = "Terms and Conditions",
                Subtitle = "Please review the legal terms governing purchases, warranties, delivery timelines, and services on EdgeTech.",
                Badge = "Legal & Compliance",
                LastUpdated = "August 2026",
                UpdatedAt = DateTime.UtcNow,
                Sections = new List<PolicySection>
                {
                    new PolicySection
                    {
                        Order = 1,
                        Title = "1. Company Information & Legal Status",
                        Body = "EdgeTech Solutions (\"EdgeTech\", \"we\", \"our\", \"us\") operates this online commerce platform specializing in professional CCTV surveillance, security systems, IP cameras, DVR/NVR hardware, networking, and comprehensive IT solutions across Bangladesh.",
                        HighlightTitle = "Registered Business Information",
                        HighlightText = "Registered Name: EdgeTech Solutions\nTrade License Number: TRAD/DNCC/042819/2023 (Dhaka North City Corporation)\nRegistered Office: 373, South Monipur, Mirpur-2, Dhaka 1216.\nDirect Contact: +880 1329-661250 | info@edgetech.com.bd"
                    },
                    new PolicySection
                    {
                        Order = 2,
                        Title = "2. Delivery & Fulfillment Timelines",
                        Body = "We partner with premier nationwide logistics couriers to ensure fast and secure doorstep delivery for all equipment orders:",
                        SubItems = new List<PolicySubItem>
                        {
                            new PolicySubItem { Title = "Inside Dhaka City", Text = "Standard delivery timeline is 5 working days from order confirmation and verification." },
                            new PolicySubItem { Title = "Outside Dhaka / Nationwide", Text = "Standard delivery timeline is 10 working days across all divisions and districts in Bangladesh." }
                        }
                    },
                    new PolicySection
                    {
                        Order = 3,
                        Title = "3. Orders, Pricing & Product Availability",
                        Body = "All product prices listed on our portal are in Bangladeshi Taka (BDT / ৳) and include applicable trade taxes unless explicitly noted. Product stock quantities are actively updated. In the rare scenario where an item becomes unavailable after placement, our customer care team will notify you immediately with equivalent options or an immediate full refund."
                    },
                    new PolicySection
                    {
                        Order = 4,
                        Title = "4. Return and Refund Guarantee",
                        Body = "Customers are protected by our standard 7 to 10 working days return and refund policy. If an item arrives physically damaged, missing parts, or non-functional, please file a claim within 48 hours. Detailed terms are available on our Return & Refund Policy page."
                    },
                    new PolicySection
                    {
                        Order = 5,
                        Title = "5. Third-Party Advertisements & Data Protection",
                        Body = "EdgeTech maintains a strictly focused commercial store. No unauthorized third-party advertisements are published on our platform. In the event any external third-party links or advertisements appear, EdgeTech assumes merchant responsibility for ensuring our platform remains secure, but cannot control third-party destinations.\n\nFurthermore, EdgeTech strictly safeguards all customer personally identifiable information (PII). We do not sell, rent, or unauthorizedly disclose customer details to any third-party advertisers. All data handling complies with our Privacy Policy."
                    },
                    new PolicySection
                    {
                        Order = 6,
                        Title = "6. Payments & EMI Terms",
                        Body = "We accept online payments via SSLCommerz certified payment gateways (Visa, MasterCard, American Express, bKash, Nagad, Rocket, Upay, and internet banking) and Cash on Delivery (COD). For eligible purchases, Equal Monthly Installment (EMI) facilities of 3, 6, 9, 12, 18, 24, or 36 months are provided in partnership with supported Bangladeshi commercial banks."
                    },
                    new PolicySection
                    {
                        Order = 7,
                        Title = "7. Governing Law & Jurisdiction",
                        Body = "These Terms & Conditions shall be governed by and construed in accordance with the laws of the People's Republic of Bangladesh. Any dispute arising out of or in connection with these terms shall be subject to the exclusive jurisdiction of the courts of Dhaka, Bangladesh."
                    }
                }
            },

            // 2. Privacy Policy
            new PolicyPage
            {
                Slug = "privacy",
                Title = "Privacy Policy",
                Subtitle = "Your privacy and security are paramount. Learn how EdgeTech collects, protects, and handles your data.",
                Badge = "Data Protection & Privacy",
                LastUpdated = "August 2026",
                UpdatedAt = DateTime.UtcNow,
                Sections = new List<PolicySection>
                {
                    new PolicySection
                    {
                        Order = 1,
                        Title = "1. Commitment to Privacy",
                        Body = "EdgeTech Solutions (\"EdgeTech\", \"we\", \"our\") is committed to protecting your personal information. This Privacy Policy details how we collect, store, utilize, and protect your information when you visit or place orders on our official website."
                    },
                    new PolicySection
                    {
                        Order = 2,
                        Title = "2. Information We Collect",
                        Body = "To process your surveillance and IT orders, verify shipping, and deliver hardware safely to your doorstep, we collect:",
                        ListItems = new List<string>
                        {
                            "Contact & Shipping Data: Full Name, delivery address, division/district, active phone number, and email address.",
                            "Order Details: Items purchased, quantity, Build Your Solution configurations, and preferred delivery time slots.",
                            "Payment Information: Transaction identifiers and payment status (card details and mobile banking PINs are processed securely by certified gateways like SSLCommerz and never stored on our servers)."
                        }
                    },
                    new PolicySection
                    {
                        Order = 3,
                        Title = "3. Third-Party Advertisements & Non-Disclosure",
                        HighlightTitle = "Strict Data Protection Standard",
                        HighlightText = "EdgeTech does not allow unauthorized third-party advertisements on our platform. We strictly do not sell, trade, monetize, or disclose customer personal information to any third-party advertisers or marketing brokers. Any information provided by customers is handled with merchant-level responsibility and used exclusively for fulfilling orders and customer support."
                    },
                    new PolicySection
                    {
                        Order = 4,
                        Title = "4. Security & Encryption Standards",
                        Body = "We employ industry-standard 256-bit SSL encryption across all browsing sessions and checkout steps. All account credentials, passwords, and administrative access points are protected with modern cryptographic hashing (PBKDF2/SHA256) and strict role-based access control."
                    },
                    new PolicySection
                    {
                        Order = 5,
                        Title = "5. Data Controller & Inquiries",
                        Body = "For any questions or data requests regarding your personal details, contact our registered compliance office:",
                        SubItems = new List<PolicySubItem>
                        {
                            new PolicySubItem
                            {
                                Title = "EdgeTech Privacy Officer",
                                Text = "Company: EdgeTech Solutions (Trade License: TRAD/DNCC/042819/2023)\nAddress: 373, South Monipur, Mirpur-2, Dhaka 1216.\nEmail: privacy@edgetech.com.bd | info@edgetech.com.bd\nDirect Helpline: +880 1329-661250"
                            }
                        }
                    }
                }
            },

            // 3. Return & Refund Policy
            new PolicyPage
            {
                Slug = "refund-policy",
                Title = "Return and Refund Policy",
                Subtitle = "We stand behind the quality of our CCTV surveillance, networking equipment, and security products.",
                Badge = "Customer Assurance",
                LastUpdated = "August 2026",
                UpdatedAt = DateTime.UtcNow,
                Sections = new List<PolicySection>
                {
                    new PolicySection
                    {
                        Order = 1,
                        Title = "1. Standard Return & Refund Timeline",
                        HighlightTitle = "7 to 10 Working Days Standard Resolution",
                        HighlightText = "All approved returns and refunds are processed within 7 to 10 working days from the date our quality assessment team receives the returned item at our Dhaka service hub.",
                        Body = "Refunds will be issued directly through the original method of payment (bKash, Nagad, Visa/MasterCard, or direct bank transfer)."
                    },
                    new PolicySection
                    {
                        Order = 2,
                        Title = "2. Eligibility for Returns & Replacements",
                        Body = "You may initiate a return or replacement request under the following circumstances:",
                        ListItems = new List<string>
                        {
                            "Defective or Damaged Products: The item is non-functional on arrival or damaged during transit.",
                            "Wrong Item Delivered: The received camera, NVR, cable, or accessory differs from the placed order description.",
                            "Missing Accessories / Components: Parts, power adapters, or mounting kits listed in the product specifications are missing."
                        }
                    },
                    new PolicySection
                    {
                        Order = 3,
                        Title = "3. Conditions for Return Acceptance",
                        SubItems = new List<PolicySubItem>
                        {
                            new PolicySubItem { Title = "Original Packaging Required", Text = "Products must be returned in their original box with all warranty cards, user manuals, accessories, and uncompromised serial numbers / barcodes." },
                            new PolicySubItem { Title = "Notification Window", Text = "Return claims must be submitted to our support team within 48 hours of receiving the package for prompt handling." }
                        }
                    },
                    new PolicySection
                    {
                        Order = 4,
                        Title = "4. How to Initiate a Return",
                        Body = "Following these 4 simple steps guarantees swift resolution:",
                        ListItems = new List<string>
                        {
                            "Step 1: Contact our helpline at +880 1329-661250 (WhatsApp or Call) or email support@edgetech.com.bd with your Order ID and photo/video of the issue.",
                            "Step 2: Our support representative will verify details and issue a Return Authorization Code.",
                            "Step 3: Hand over the securely packaged parcel to our designated courier pickup or drop it off at our Multiplan Center location.",
                            "Step 4: Once our technical team completes inspection (within 48 hours of receipt), replacement shipment or refund will be dispatched within 7-10 working days."
                        }
                    }
                }
            },

            // 4. About Us & Management
            new PolicyPage
            {
                Slug = "about",
                Title = "About EdgeTech",
                Subtitle = "Empowering businesses, homes, and institutions across Bangladesh with smart surveillance, enterprise networking, and modern IT infrastructure.",
                Badge = "Company Profile",
                LastUpdated = "",
                UpdatedAt = DateTime.UtcNow,
                Sections = new List<PolicySection>
                {
                    new PolicySection
                    {
                        Order = 1,
                        Title = "Company Overview & Legal Identity",
                        Body = "Founded in 2015, EdgeTech Solutions has grown to become one of Bangladesh's premier authorized distributors and system integrators for top-tier security and surveillance hardware, including Dahua, Hikvision, Uniview, TP-Link, and Western Digital.",
                        HighlightTitle = "Mandatory Trade License & Registration Details",
                        HighlightText = "Company Name: EdgeTech Solutions\nTrade License Number: TRAD/DNCC/042819/2023 (DNCC Registered)\nBusiness Type: Information Technology, CCTV Surveillance & Electronic Security Equipment\nRegistered Office: 373, South Monipur, Mirpur-2, Dhaka 1216.\nOfficial Hotline: +880 1329-661250 | Email: info@edgetech.com.bd"
                    },
                    new PolicySection
                    {
                        Order = 2,
                        Title = "Executive Management & Leadership",
                        Body = "Our experienced leadership brings decades of collective expertise in electronic surveillance engineering, telecom infrastructure, and customer success:",
                        SubItems = new List<PolicySubItem>
                        {
                            new PolicySubItem { Title = "Naimur Rahman Ayon", Subtitle = "Chief Executive Officer & Founder", Text = "Oversees overall strategic vision, global vendor partnerships, and nationwide enterprise installations.", Tag = "AT" },
                            new PolicySubItem { Title = "Atonu Ahmed", Subtitle = "Head of Engineering & Solutions", Text = "Leads system architecture, CCTV package engineering, firmware quality assurance, and technical support.", Tag = "SR" },
                            new PolicySubItem { Title = "Tahmina Haque Luva", Subtitle = "Director of Operations & Compliance", Text = "Drives customer experience, logistics fulfillment, warranty servicing, and regulatory compliance.", Tag = "NH" }
                        }
                    },
                    new PolicySection
                    {
                        Order = 3,
                        Title = "Our Core Commitments",
                        SubItems = new List<PolicySubItem>
                        {
                            new PolicySubItem { Title = "100% Genuine Products", Text = "Every camera, NVR, DVR, and cable is sourced directly from certified authorized manufacturers with official Bangladesh warranty." },
                            new PolicySubItem { Title = "Transparent Timelines", Text = "Guaranteed delivery within 5 days in Dhaka and 10 days nationwide, backed by our 7 to 10 working days return & refund assurance." },
                            new PolicySubItem { Title = "Flexible Payment & EMI", Text = "Secure payments through SSLCommerz, bKash, Nagad, and 3 to 36 months EMI facilities across major Bangladeshi banks." },
                            new PolicySubItem { Title = "Certified Support", Text = "Dedicated technical engineers ready to assist with remote diagnostics, on-site setup, and Build Your Solution recommendations." }
                        }
                    }
                }
            },

            // 5. Contact Us
            new PolicyPage
            {
                Slug = "contact",
                Title = "Contact & Registered Office",
                Subtitle = "Have questions about CCTV surveillance systems, custom packages, order tracking, or warranty support? We are here to help.",
                Badge = "Get in Touch",
                LastUpdated = "",
                UpdatedAt = DateTime.UtcNow,
                Sections = new List<PolicySection>
                {
                    new PolicySection
                    {
                        Order = 1,
                        Title = "Registered Business Details",
                        HighlightTitle = "Trade License & Registered Address (DNCC)",
                        HighlightText = "Company Name: EdgeTech Solutions\nTrade License No: TRAD/DNCC/042819/2023\nRegistered Office: 373, South Monipur, Mirpur-2, Dhaka 1216."
                    },
                    new PolicySection
                    {
                        Order = 2,
                        Title = "Direct Channels & Helplines",
                        SubItems = new List<PolicySubItem>
                        {
                            new PolicySubItem { Title = "WhatsApp Direct Support", Text = "+880 1329-661250 (Click to Chat)\nInstant live chat assistance for product consultation and order inquiries." },
                            new PolicySubItem { Title = "Official Hotline", Text = "+880 1329-661250\nDirect voice support available during operating hours." },
                            new PolicySubItem { Title = "Email Desk", Text = "info@edgetech.com.bd\nInquiries, wholesale quotation, and formal enterprise proposals." },
                            new PolicySubItem { Title = "Business Hours", Text = "Saturday – Thursday: 9:00 AM – 8:00 PM\nFriday: Closed (Online Orders Processed Next Day)" }
                        }
                    }
                }
            }
        };
    }

    public static async Task SeedAsync(MongoDbContext db)
    {
        var existingSlugs = (await db.PolicyPages.Find(_ => true)
            .Project(p => p.Slug)
            .ToListAsync()).ToHashSet();

        var defaults = GetDefaultPages();
        foreach (var page in defaults)
        {
            if (!existingSlugs.Contains(page.Slug))
            {
                await db.PolicyPages.InsertOneAsync(page);
            }
        }
    }
}
