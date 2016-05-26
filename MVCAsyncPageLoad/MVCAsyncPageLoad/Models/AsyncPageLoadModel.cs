using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MVCAsyncPageLoad.Models
{
    public class AsyncPageLoadModel
    {
        /// <summary>
        /// Represent the callback action.
        /// </summary>
        public string TargetURL { get; set; }

        /// <summary>
        /// The HTTP method used by client's ajax call.
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// ID of div control which used to show loading icon.
        /// If you use AsyncPageLoad more than one time in a page, 
        /// make sure those fields has differnt values.
        /// </summary>
        public string DivContentID { get; set; }

        public string ParametersInJSON { get; set; }
    }
}
