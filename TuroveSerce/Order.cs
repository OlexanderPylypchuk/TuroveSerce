using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuroveSerce.Bot
{
	public class Order
	{
		public string Item {  get; set; }
		public string DeliveryMethod { get; set; }
		public string City { get; set; }
		public string PhoneNumber { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Count { get; set; }
		public string ReferalCode { get; set; }
		public string ChatId { get; set; }
	}
}
