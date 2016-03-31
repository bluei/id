using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class JobMasterBOM
    {
        partial void StandardCost_Compute(ref decimal result)
        {
            // get standard cost from product
            if (this.Product != null)
            {
                result = this.Product.StandardCost;
            }
            else
            {
                result = (decimal)0;
            }
        }

        partial void GrossMargin_Compute(ref decimal result)
        {
            // http://www.calculatorsoup.com/calculators/financial/margin.php
            // The gross margin percentage is the profit divided by the selling price.

            if (Price > 0)
            {
                result = Math.Round((GrossProfit / Price),4);
            }
            else
            {
                result = 0M;
            }

        }

        partial void ActualCost_Compute(ref decimal? result)
        {
            // Calculated as the sum of all JMBI Extended costs
            if (this.JobMasterBOMItem.Count() > 0)
            {
                result = this.JobMasterBOMItem.Sum(ec => ec.ExtActCost);
            }

        }

        partial void GrossProfit_Compute(ref decimal result)
        {
            // The gross profit P is the difference between the cost to make a product C and the selling price or revenue R. 
            result = this.Price - this.ActualCost.GetValueOrDefault();
        }

        partial void JobMasterBOM_Created()
        {
            this.Price = 0;
        }

        partial void JMBDisplay_Compute(ref string result)
        {
            if (this.Product != null)
            {
                result = this.Product.PartNumber;
            }
        }
    }
}
