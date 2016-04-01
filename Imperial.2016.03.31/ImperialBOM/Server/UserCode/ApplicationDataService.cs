using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Security.Server;
using System.Linq.Expressions;

namespace LightSwitchApplication
{
    public partial class ApplicationDataService
    {
        private static List<ProductListItem> _listOfProductListItems;
        private class ProductListItem
        {
            // class used to create a flat BOM list for processing deleted BOM items out of the JMBIs
            // when regenerating BOM
            public int ProductId { get; set; }
            public string ProductPath { get; set; }
        }
        #region "PRE_PROCESS QUERIES"

        partial void Companies_All_PreprocessQuery(ref IQueryable<Company> query)
        {
            query = (from company in query
                     orderby company.Name
                     select company);
        }
        partial void CompaniesWithJobs_PreprocessQuery(ref IQueryable<Company> query)
        {
            query = (
                from c in query
                where c.Jobs.Count() > 0
                select c);
        }
        partial void JobMasterBOMItemsSearch_PreprocessQuery(int? ParamJobMasterBOMId, string ParamOptLevel, int? ParamOptDepth, ref IQueryable<JobMasterBOMItem> query)
        {
            if (ParamOptDepth.HasValue)
            {
                query = query
                .Where(q => q.Depth <= ParamOptDepth)
                .OrderBy(o=>o.Level)
                .ThenBy(o=>o.Id);
            }

            if (!string.IsNullOrEmpty(ParamOptLevel))
            {
                query = query
                .Where(q => q.Level.StartsWith(ParamOptLevel))
                .OrderBy(o => o.Level)
                .ThenBy(o => o.Id);
            }
        }
        partial void JobStatuses_All_PreprocessQuery(ref IQueryable<JobStatus> query)
        {
            query = (from js in query
                     orderby js.Sort
                     select js);
        }
        partial void Messages_All_PreprocessQuery(ref IQueryable<Message> query)
        {
            query = (from m in query
                     orderby m.Priority ascending, m.Created descending
                     select m);
        }
        partial void OpenPurchaseOrders_PreprocessQuery(ref IQueryable<PurchaseOrder> query)
        {
            query = query.OrderBy(p => p.PurchaseOrderStatus.Seq).ThenBy(p => p.DateDue);
        }
        partial void PurchaseOrderStatuses_All_PreprocessQuery(ref IQueryable<LightSwitchApplication.PurchaseOrderStatus> query)
        {
            query = (from s in query
                     orderby s.Seq ascending
                     select s);
        }

        partial void VendorsWithOpenPOs_PreprocessQuery(ref IQueryable<Company> query)
        {
            query = (from company in query
                     orderby company.Name
                     where company.PurchaseOrders.Where(po => po.PurchaseOrderStatus.IsClosed == false).Count()>0
                        && company.CompanyType == "V"
                     select company);
        }
        #endregion

        #region "INSERT / UPDATE / DELETE"

        partial void BillOfMaterials_Updating(BillOfMaterial entity)
        {
            entity.ModifiedBy = Application.User.FullName;
            entity.ModifiedDate = DateTime.Now;
        }
        partial void BillOfMaterials_Deleting(BillOfMaterial entity)
        {
            Product product = entity.BOMProductAssemblyID;
            UpdateParentProductCost(ref product);
        }

        partial void Products_Updating(Product entity)
        {
            entity.ModifiedBy = Application.User.Name;
            entity.ModifiedDate = DateTime.Now;

            if (entity.Details.Properties.StandardCost.IsChanged)
            {
                // update all non-locked PO lines, which in turn will update the associated jmbi records
                var relatedPOLinesNotLocked = entity.PurchaseOrderLines.Where(p => p.PurchaseOrder.PurchaseOrderStatus.IsLocked == false).Where(p => p.Product == entity);
                foreach (var poline in relatedPOLinesNotLocked)
                {
                    if (poline.Price != entity.StandardCost) { poline.Price = entity.StandardCost; } 
                }
                UpdateParentProductCost(ref entity);
            }
        }
        private void UpdateParentProductCost(ref Product productEntity)
        {
            //productEntity.ModifiedBy = Application.User.FullName;
            //productEntity.ModifiedDate = DateTime.Now;

            // UPDATE STD COST FOR ALL PARENTS AND THEIR PARENTS
            if (productEntity.Details.Properties.StandardCost.IsChanged == true)
            {
                // entity.BOMsUsedIn is the collection of BOMs of which this product is the ComponentID
                // for each parent part that the component makes...
                foreach (BillOfMaterial bomUsedIn in productEntity.BOMsUsedIn)
                {
                    decimal total = 0m;
                    Product parentProduct = bomUsedIn.BOMProductAssemblyID;

                    // add up the standard costs for parent
                    foreach (BillOfMaterial bom in parentProduct.BOMComponents.Where(c=>c.BOMComponentID.IsActive == true))
                    {
                        total += bom.BOMComponentID.StandardCost * bom.PerAssemblyQty;
                    }

                    parentProduct.StandardCost = total;
                }
            }
        }

        partial void PurchaseOrders_Updating(PurchaseOrder entity)
        {
            if (entity.Details.Properties.PurchaseOrderStatus.IsChanged == true)
            {
                // If PO status changes from a non-closed status to a closed status, stamp the closed date
                if (entity.Details.Properties.PurchaseOrderStatus.OriginalValue.IsClosed == false &&
                    entity.Details.Properties.PurchaseOrderStatus.Value.IsClosed == true)
                {
                    entity.DateClosed = DateTime.Today;
                }
                
                // If PO Status changes, for each PO Line, update the jmbi statuses to match. 
                foreach (PurchaseOrderLine poline in entity.PurchaseOrderLines.Where(p=>p.JobMasterBOMItemId.HasValue))
                {
                    // update the jmbi status if the PO line HAS a jmbi Id, AND if jmbi status is NOT CLOSED.
                    //JobMasterBOMItem jmbi = this.DataWorkspace.ApplicationData.JobMasterBOMItems_SingleOrDefault(poline.JobMasterBOMItemId);
                    JobMasterBOMItem jmbi = this.DataWorkspace.ApplicationData.JobMasterBOMItems.Where(i => i.Id == poline.JobMasterBOMItemId).FirstOrDefault();

                    if (jmbi != null)
                    {
                            jmbi.PurchaseOrderStatus = entity.PurchaseOrderStatus;
                    }
                }
            }
        }
        partial void PurchaseOrderLines_Inserted(PurchaseOrderLine entity)
        {
            if (entity.JobMasterBOMItemId.HasValue)
            {
                // when inserting a new PO Line, if POLine jmbi has a value, add the PO Line Id to the associated JMBI record.
                JobMasterBOMItem jmbi = JobMasterBOMItems.Where(i => i.Id == entity.JobMasterBOMItemId).FirstOrDefault();
                if (jmbi != null)
                {
                    jmbi.PurchaseOrderStatus = entity.PurchaseOrder.PurchaseOrderStatus;
                    jmbi.POLineId = entity.Id;
                }
            }
        }
        partial void PurchaseOrderLines_Updating(PurchaseOrderLine entity)
        {
            // If the PO Line price changes, update the jmbi unit cost if there is a jmbi.
            if (entity.Details.Properties.Price.IsChanged == true && entity.JobMasterBOMItemId.HasValue)
            {
                JobMasterBOMItem poLineJMBI = JobMasterBOMItems.Where(i => i.Id == entity.JobMasterBOMItemId).FirstOrDefault();
                if (poLineJMBI != null)
                {
                    poLineJMBI.UnitCost = entity.Price;
                }
            }

            // if the po line was marked complete, update the jmbi status
            // get the first "closed" status out of PurchaseOrderStatuses
            if (entity.Details.Properties.IsComplete.IsChanged == true && 
                entity.IsComplete == true &&
                entity.JobMasterBOMItemId.HasValue)
            { 
                PurchaseOrderStatus firstClosedStatus = PurchaseOrderStatuses.Where(pos => pos.IsClosed == true).OrderBy(pos => pos.Seq).FirstOrDefault();
                JobMasterBOMItem jmbi = JobMasterBOMItems.Where(i => i.Id == entity.JobMasterBOMItemId).FirstOrDefault();
                if (jmbi != null)
                {
                    if (jmbi.PurchaseOrderStatus.Seq < firstClosedStatus.Seq)
                    {
                        jmbi.PurchaseOrderStatus = firstClosedStatus;
                    }
                }
            }

            // if the po line was UN-marked complete, update the jmbi status to the PO status
            if (entity.Details.Properties.IsComplete.IsChanged == true &&
                entity.IsComplete == false &&
                entity.JobMasterBOMItemId.HasValue)
            {
                JobMasterBOMItem jmbi = JobMasterBOMItems.Where(i => i.Id == entity.JobMasterBOMItemId).FirstOrDefault();
                if (jmbi != null) jmbi.PurchaseOrderStatus = entity.PurchaseOrder.PurchaseOrderStatus;
            }
        }
        partial void PurchaseOrderLines_Deleting(PurchaseOrderLine entity)
        {
            // When deleting a PO Line:
            // - cascade delete any receipts
            // - if there is a jmbi id tied to it:
            //   - remove the POLineId from the jmbi
            //   - set jmbi status to first status
            //   - if jmbi status is/was locked, add "** PO Line ID: <> deleted on <date> ** " to the line note
            // TODO: test this!

            if (entity.JobMasterBOMItemId.HasValue)
            {
                // materialize the associated JMBI
                JobMasterBOMItem jmbi = JobMasterBOMItems.Where(i => i.Id == entity.JobMasterBOMItemId).FirstOrDefault();
                if (jmbi != null)
                {
                    // Clear the POLineId from the JMBI & note the deletion if there was one.
                    if (jmbi.PurchaseOrderStatus.IsLocked)
                    {
                        // if PurchaseOrder was deleted (and this is running as a result of cascade deletes), 
                        // the PO will be null and can't be displayed. 
                        if (entity.PurchaseOrder == null)
                        {
                            string note = string.Format("■ The PO for this item was deleted on {0} by {1}.",
                                DateTime.Today.ToShortDateString(), Application.User.FullName);
                            if (!jmbi.IsDeletedInBOM) jmbi.Note = note;
                            var message = Messages.AddNew();
                            message.Job = jmbi.JobMasterBOM.Job;
                            message.Topic = "PO Line for Job BOM Item was deleted";
                            message.Note = string.Format("■ PO Line Id {0} was deleted for {1} in Master BOM {2} at Level {3} on {4} by {5}.",
                                entity.Id,
                                jmbi.Product.PartNumber,
                                jmbi.JobMasterBOM.Product.PartNumber,
                                jmbi.Level,
                                DateTime.Today.ToShortDateString(),
                                Application.User.FullName);                           
                        }
                        else
                        {
                            string note = string.Format("■ PO Line Id {0} on PO {1} was deleted for this item on {2} by {3}.",
                                entity.Id, entity.PurchaseOrder.Number, DateTime.Today.ToShortDateString(), Application.User.FullName);
                            entity.PurchaseOrder.InternalNote = note;

                                var message = Messages.AddNew();
                                message.Job = jmbi.JobMasterBOM.Job;
                                message.Topic = string.Format("PO Line Deleted", jmbi.Id);
                                message.Note = string.Format("■ PO Line Id {0} on PO {1} was deleted for {2} (in Master BOM {3} at Level {4}) on {5} by {6}.",
                                    entity.Id,
                                    entity.PurchaseOrder.Number,
                                    entity.Product.PartNumber,
                                    jmbi.JobMasterBOM.Product.PartNumber,
                                    jmbi.Level,
                                    DateTime.Today.ToShortDateString(),
                                    Application.User.FullName);

                            // if this was the last or only line on the PO, set status to created.
                            if (entity.PurchaseOrder.PurchaseOrderLines.Count() == 0)
                                entity.PurchaseOrder.PurchaseOrderStatus = PurchaseOrderStatuses.FirstOrDefault();
                        }
                    }
                    // whether status is locked or not, deleting a po line with a jmbi clears the PoLineId and resets status to created...
                    // unless the jmbi is going to be deleted 
                    if (!jmbi.IsDeletedInBOM)
                    {
                        jmbi.POLineId = null;
                        jmbi.PurchaseOrderStatus = this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.OrderBy(ps => ps.Seq).FirstOrDefault();
                    }
                    
                }
            }
        }

        partial void Jobs_Inserted(Job entity)
        {
            // after the job is inserted, update the company's next job number
            // this will put the company in an edited mode
            entity.Company.NextJobNumber++;
        }

        partial void JobMasterBOMItems_Updating(JobMasterBOMItem entity)
        {
            // if there is a PO Line and not at a locked status,
            // if the extended quantity has changed, update the quantity on the PO Line
            // note that changes to the actual cost (price) are pushed from the PO Line to the jmbi actual, not the reverse.

            // if jmbi QTY is changed, not locked, and there is a PO line, update PO line qty.
            if (entity.POLineId.HasValue && 
                entity.PurchaseOrderStatus.IsLocked == false &&
                entity.Details.Properties.ExtendedQty.IsChanged)
            {
                PurchaseOrderLine poLine = PurchaseOrderLines.Where(l => l.Id == entity.POLineId).FirstOrDefault();
                if (poLine != null)
                {
                    poLine.OrderQty = entity.ExtendedQty.GetValueOrDefault(); 
                }
            }

            // if jmbi COST is changed, status not locked, and there is a PO line, update PO Price.
            // ** this is handled in Products_Updating

            // if the jmbi line was deleted, then set the status back to first (created)
            if (entity.Details.Properties.POLineId.IsChanged &&
                entity.POLineId == null)
            {
                entity.PurchaseOrderStatus = PurchaseOrderStatuses.OrderBy(s => s.Seq).FirstOrDefault();
            }


        }

        #endregion

        #region "ACCESS METHODS"

        partial void Companies_CanDelete(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.DeleteCompanies);
        }
        partial void Invoices_CanDelete(ref bool result)
        {
            Application.User.HasPermission(Permissions.DeleteInvoices);
        }
        partial void ProductTypes_CanDelete(ref bool result)
        {
            result = false;
        }

        #endregion

        partial void Products_Validate(Product entity, EntitySetValidationResultsBuilder results)
        {
            // warn if standard cost is zero
            if (entity.StandardCost == 0)
            {
                results.AddPropertyResult
                    ("Note: Standard Cost is zero.  Are you sure?", ValidationSeverity.Warning, entity.Details.Properties.StandardCost);
            }
        }        
        partial void JobMasterBOMItems_Validate(JobMasterBOMItem entity, EntitySetValidationResultsBuilder results)
        {
            // Validate Deletions  ** see pg 141 and do this on the server in 3-step process
            if (entity.Details.EntityState == EntityState.Deleted && !entity.IsDeletedInBOM)
            {
                results.AddEntityError("Cannot delete if there is a PO Line. Please delete the PO line first.");
            }
        }
        partial void PurchaseOrderLines_Validate(PurchaseOrderLine entity, EntitySetValidationResultsBuilder results)
        {
            // If all PO Lines are completed but PO is not, complete the PO
            if (!entity.PurchaseOrder.PurchaseOrderStatus.IsClosed)
            {
                int polCount = entity.PurchaseOrder.PurchaseOrderLines.Count();
                if (polCount == entity.PurchaseOrder.PurchaseOrderLines.Where(pol => pol.IsComplete == true).Count())
                {
                    // if there are po lines, and all are complete, move the PO Status to the first complete status
                    if (polCount > 0)
                    {
                        entity.PurchaseOrder.PurchaseOrderStatus = PurchaseOrderStatuses
                        .Where(pos => pos.IsClosed == true).OrderBy(pos => pos.Seq).FirstOrDefault();
                    }
                    else
                    {
                        // if there are no lines, then they were deleted.  set status to first and clear date ordered and due
                        entity.PurchaseOrder.PurchaseOrderStatus = PurchaseOrderStatuses.FirstOrDefault(); //UserCode.Globals._initialPOStatus;
                        entity.PurchaseOrder.DateOrdered = null;
                        entity.PurchaseOrder.DateDue = null;
                    }
                }
            }
        }

        partial void ContactsByCompany_PreprocessQuery(int? company_id, ref IQueryable<Contact> query)
        {
            query = query.OrderBy(l => l.LastName);
        }

        partial void BillOfMaterials_Validate(BillOfMaterial entity, EntitySetValidationResultsBuilder results)
        {
            // make sure we don't add duplicate components
            if (entity.Details.EntityState == EntityState.Added)
            {
                int i = entity.BOMProductAssemblyID.BOMComponents.Where(p => p.BOMComponentID == entity.BOMComponentID).Count();
                if (i >1)
                {
                    //results.AddEntityError("This component is a duplicate.");
                    results.AddEntityResult("Note: this component is a duplicate.",ValidationSeverity.Informational);
                }
            }
        }

        partial void Explosions_Inserting(Explosion entity)
        {
            // call the procedure from the Exploding Class on server side (like Central.Utilities)
            //var tempDW = Application.CreateDataWorkspace();
            JobMasterBOM jobMasterBOM = JobMasterBOMs
                .Where(j => j.Id == entity.JobMasterBomId).FirstOrDefault();
            if (jobMasterBOM != null)
            {
                switch (entity.Action)
                {
                    case 1:
                        // STEP 1 - Explode the BOM and add to the records
                        ExplodeMasterBOM(jobMasterBOM, jobMasterBOM.Product, 1, 1m, string.Empty,
                                jobMasterBOM.Product.PartNumber.ToString() + ">", string.Empty);
                        break;
                    case 2:
                        // STEP 2 - process duplicates
                        ProcessDuplicates(jobMasterBOM);
                        break;
                    case 3:
                        // preocess deleted items
                        ProcessDeletions(jobMasterBOM);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ExplodeMasterBOM(JobMasterBOM jobMasterBOM, Product product, int depth, decimal extendedQty, string level,
                                  string partsPath, string idPath)
        {
            //var tempDW = Application.CreateDataWorkspace();

            // given a product, this method recursively calls itself for each subcomponent (and in turn their subcomponents)
            var sortedBOM = (from BillOfMaterial cb in product.BOMComponents
                             orderby (cb.BOMComponentID.ProductGroup == null ? 0 : cb.BOMComponentID.ProductGroup.Seq),
                                      cb.BOMComponentID.PartNumber
                             select cb);
            int count = 0;
            if (depth > 1)
            {
                level += ".";
                partsPath += ">";
                idPath += ", ";
            }

            foreach (BillOfMaterial component in sortedBOM)
            {
                count++;
                string addon = count.ToString().PadLeft(2, '0');
                string partNumber = component.BOMComponentID.PartNumber;
                string idNumber = component.BOMComponentID.Id.ToString();
                decimal perAssemblyQuantity = component.PerAssemblyQty;

                //JobMasterBOMItem jmbi = new JobMasterBOMItem();
                JobMasterBOMItem jmbi = JobMasterBOMItems.AddNew();

                jmbi.JobMasterBOM = jobMasterBOM;
                // note: jmbi.PurchaseOrderStatus is set in the created method of the data
                jmbi.Depth = depth;
                jmbi.Product = component.BOMComponentID;
                jmbi.Vendor = component.BOMComponentID.DefaultVendor;
                jmbi.PerAssemblyQty = component.PerAssemblyQty;
                jmbi.UnitCost = component.BOMComponentID.StandardCost;
                jmbi.ExtendedQty = extendedQty * perAssemblyQuantity;
                jmbi.Level = level + addon;
                jmbi.PartsPath = partsPath + partNumber;
                jmbi.IdPath = idPath + idNumber;

                // recursively call the procedure until no children
                ExplodeMasterBOM(jobMasterBOM, component.BOMComponentID, depth + 1, component.PerAssemblyQty * extendedQty,
                    level + addon, partsPath + partNumber, idPath + idNumber);

            }     
        }

        private static void ProcessDuplicates(JobMasterBOM jmb)
        {
            // start processing original jmbi's first
            var jmbiList = jmb.JobMasterBOMItem.OrderBy(o => o.Id);
            bool doneProcessing = false;

            foreach (JobMasterBOMItem jmbi in jmbiList)
            {
                if (!doneProcessing)
                {
                    // see if another exists that has the same IdPath that has not already been processed
                    JobMasterBOMItem jmbiAdded = jmb.JobMasterBOMItem
                        .Where(j => j.Id != jmbi.Id && j.IdPath == jmbi.IdPath && j.Processed == false)
                        .FirstOrDefault();

                    // we only want to compare the origial jmbi's against the new ones added, otherwise the routine
                    // would continue and compare the addded against the new.. also stops the query above from running on added items
                    // this would not be needed if done while rebuilding
                    if (jmbiAdded != null && jmbiAdded.Id < jmbi.Id) doneProcessing = true;

                    if (jmbiAdded != null && !doneProcessing)  // Id used to prevent comparing the added items with the original. just want the original comared to added.
                    // A duplicate exists, so deal with it
                    {

                        // ANY Status, locked or not...
                        if (jmbi.Level != jmbiAdded.Level)
                        {
                            jmbi.Level = jmbiAdded.Level;
                        }

                        // ----- COST has changed...
                        if (jmbi.UnitCost != jmbiAdded.UnitCost)
                        {
                            //if it is a parent, then just update it, otherwise, NOTE that it changed.
                            if (jmbiAdded.Product.ProductType.CanHaveBOM)
                            {
                                jmbi.UnitCost = jmbiAdded.UnitCost;
                            }
                            else
                            {
                                // if no PO and not closed, update it.  Otherwise just note it.
                                if (jmbi.POLineId.HasValue == false && jmbi.PurchaseOrderStatus.IsClosed == false && jmbi.PurchaseOrderStatus.IsOrdered==false)
                                {
                                    jmbi.UnitCost = jmbiAdded.UnitCost;
                                }
                                else
                                {
                                    jmbi.Note = "■ Note: Product standard cost is " + jmbiAdded.Product.StandardCost.ToString();
                                }
                            }
                            
                            // if QTY did not change, then delete the added jmbi - no need to do the routine below.
                            if (jmbi.PerAssemblyQty == jmbiAdded.PerAssemblyQty
                                    && jmbi.ExtendedQty == jmbiAdded.ExtendedQty.GetValueOrDefault())
                            {
                                jmbiAdded.Processed = true;
                                jmbiAdded.Delete();
                            }
                        }

                        Debug.WriteLine(string.Format("##### jmbi={0} Qty:{1} | jmbiAdded={2} Qty:{3}", jmbi.Id, jmbi.UnitCost, jmbiAdded.Id, jmbiAdded.UnitCost));

                        // ----- QTY has changed...
                        if (jmbi.PerAssemblyQty != jmbiAdded.PerAssemblyQty
                                || jmbi.ExtendedQty.GetValueOrDefault() != jmbiAdded.ExtendedQty.GetValueOrDefault())
                        {
                            if (!jmbi.PurchaseOrderStatus.IsLocked)
                            {
                                // if not locked then just update the current PAQ and delete the added - no conflict
                                jmbi.PerAssemblyQty = jmbiAdded.PerAssemblyQty;
                                jmbi.ExtendedQty = jmbiAdded.ExtendedQty.GetValueOrDefault();
                                // po req qty?
                                jmbiAdded.Processed = true;
                                jmbiAdded.Delete();
                            }
                            else
                            {
                                jmbi.PerAssemblyQtyRegenerated = jmbiAdded.PerAssemblyQty;
                                jmbi.ExtendedQtyRegenerated = jmbiAdded.ExtendedQty.GetValueOrDefault();
                                if (jmbi.PerAssemblyQty!=jmbi.PerAssemblyQtyRegenerated)
                                    jmbi.Note = string.Format("■■ BOM Qty Per Assy is now {0}.  ", jmbi.PerAssemblyQtyRegenerated);
                                if (jmbi.ExtendedQty.GetValueOrDefault() != jmbi.ExtendedQtyRegenerated.GetValueOrDefault())
                                    jmbi.Note += string.Format("■■ BOM Extended Qty is now {0}", jmbi.ExtendedQtyRegenerated.GetValueOrDefault());
                                jmbi.Note += " ■■";
                                if (jmbi.Note.Length > 255)
                                {
                                    jmbi.Note = jmbi.Note.Substring(0, 250);
                                }
                                jmbiAdded.Processed = true;
                                jmbiAdded.Delete();
                            }
                        }
                        else
                        {
                            // if they were a discrepancy but now are not, clear them
                            if (jmbi.PerAssemblyQtyRegenerated != null) jmbi.PerAssemblyQtyRegenerated = null;
                            jmbi.ExtendedQtyRegenerated = null;
                        }

                        if (jmbi.PerAssemblyQty == jmbiAdded.PerAssemblyQty && jmbi.ExtendedQty.GetValueOrDefault() == jmbiAdded.ExtendedQty.GetValueOrDefault() && jmbi.UnitCost == jmbiAdded.UnitCost)
                        {

                            jmbiAdded.Processed = true;
                            jmbiAdded.Delete();
                        }

                    }// jmbiOther != null
                }//done processing
            }// foreach jmbi
        }

        private static void ProcessDeletions(JobMasterBOM jmb)
        {
            // generate a list of IdPaths for all items in the current BOM
            List<ProductListItem> list = GetBOMProductListItems(jmb.Product);

            /// Debugging code for list...
            //string message = string.Empty;
            //foreach (var item in list)
            //{
            //    message += string.Format("{0}\n", item.ProductPath);
            //}
            //this.ShowMessageBox(message);

            foreach (JobMasterBOMItem j in jmb.JobMasterBOMItem)
            {
                // if the JMBI is not contained in the BOM Products list (BOM component was deleted)...
                bool exists = list.Any(pli => pli.ProductPath == j.IdPath);
                if (!exists)
                {
                    if (!j.PurchaseOrderStatus.IsClosed &&
                        !j.PurchaseOrderStatus.IsLocked &&
                        !j.PurchaseOrderStatus.IsOrdered)
                    {
                        // just delete it if nothing has been done.
                        j.Delete();
                    }
                    else
                    {
                        // otherwise, note as a conflict
                        j.IsDeletedInBOM = true;
                        j.Note = "■■ This component was deleted from the BOM structure. ■■";
                    }
                }
                else
                {
                    if (j.IsDeletedInBOM==true) j.IsDeletedInBOM = false;
                }
            }
        }

        private static List<ProductListItem> GetBOMProductListItems(Product masterBOMProduct)
        {
            // Wrapper for recursive procedure below.  Clears the _listOfProducts variable, then explodes a product's BOM
            // into a list of product id's and paths.  It is used to find jmbi items that are no longer in the BOM.

            _listOfProductListItems = null;
            _listOfProductListItems = new List<ProductListItem>();
            BuildListOfProductListItems(masterBOMProduct, masterBOMProduct.Id.ToString());
            return _listOfProductListItems;
        }

        private static void BuildListOfProductListItems(Product product, string idPath)
        {
            // Recursive procedure initiated by GetBOMProducts()
            ProductListItem productListItem = new ProductListItem();
            productListItem.ProductId = product.Id;
            if (product.Id.ToString() == idPath) // skips adding the initial master product to the list
            {
                productListItem.ProductPath = string.Empty;  //if equal, don't add the id, id to the path
            }
            else
            {
                // if first node, dont prefix path with comma, otherwise add comma and current id
                productListItem.ProductPath = (string.IsNullOrEmpty(idPath) ? product.Id.ToString() : idPath + ", " + product.Id);
               _listOfProductListItems.Add(productListItem);
            }

            foreach (BillOfMaterial component in product.BOMComponents.OrderBy(p => p.BOMComponentID.PartNumber))
            {
                BuildListOfProductListItems(component.BOMComponentID, productListItem.ProductPath);  //second param was idPath + component.BOMComponentID.Id.ToString()
            }
            return;
        }


    }
}