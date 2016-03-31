using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;

using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;
using Microsoft.LightSwitch.Threading;

namespace LightSwitchApplication
{
    public partial class JobDetail
    {
        //C1.Silverlight.FlexGrid.C1FlexGrid _flex;
        private int? _maxDepth;
        private DataGrid _dg;
        private bool _loadedOnce = false;
        private bool _validatedOnce = false;
        private IContentItemProxy jmbiDG;
        private IContentItemProxy poDG;

        partial void JobDetail_Created()
        {
            this.FindControl("PurchaseOrders").ControlAvailable += Application.HideGridHeaderBottom;
            this.FindControl("PurchaseOrders").ControlAvailable += Application.HideGridHeaderTop;
            this.FindControl("InvoicesGrid").ControlAvailable += Application.HideGridHeaderBottom;
            this.FindControl("InvoicesGrid").ControlAvailable += Application.HideGridHeaderTop;

            this.FindControl("ProcessDuplicates").IsVisible = Application.User.HasPermission(Permissions.ProcessDuplicatesFirst);

            jmbiDG = this.FindControl("JobMasterBOMItemsSearch");
            jmbiDG.ControlAvailable += new EventHandler<ControlAvailableEventArgs>(JobMasterBOMItemsSearch_ControlAvailable);  // TODO: check for null
            Debug.WriteLine("%%%%%% jmbiDG.ControlAvailable completed");
            
            // PO Section Color Coding
            poDG = this.FindControl("PurchaseOrders");
            poDG.ControlAvailable += new EventHandler<ControlAvailableEventArgs>(PurchaseOrdersGrid_ControlAvailable);
        }
        private void PurchaseOrdersGrid_ControlAvailable(object sender, ControlAvailableEventArgs e)
        {
            IContentItem contentItem = (e.Control as Control).DataContext as IContentItem;
            foreach (var item in contentItem.ChildItems.First().ChildItems)
            {
                IContentItemProxy CtrlBinder = this.FindControl(item.BindingPath);
                CtrlBinder.SetBinding(TextBlock.ForegroundProperty, "Details.Entity.PurchaseOrderStatus.FgColor", new ChangeFontColorStatus(), BindingMode.TwoWay);
            }
        }
        private void JobMasterBOMItemsSearch_ControlAvailable(object sender, ControlAvailableEventArgs e)
        {

            IContentItem contentItem = (e.Control as Control).DataContext as IContentItem;
            foreach (var item in contentItem.ChildItems.First().ChildItems)
            {
                IContentItemProxy CtrlBinder = this.FindControl(item.BindingPath);
                CtrlBinder.SetBinding(TextBlock.ForegroundProperty, "Details.Entity.PurchaseOrderStatus.FgColor", new ChangeFontColorStatus(), BindingMode.TwoWay);
            }
            
            
            var ctrl = e.Control as DataGrid;
            _dg = ctrl;
            if (ctrl != null)
                ctrl.LoadingRow += ctrl_LoadingRow;
            //SortGridColumns();
        }
        private void ctrl_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var grid = sender as DataGrid;
            var row = e.Row as DataGridRow;
            DataGridCell changeCell;

            var bindingVendor = new Binding("NeedsVendor")
            {
                Mode = BindingMode.OneWay,
                Converter = new HighlightColorConverter(),
                ValidatesOnExceptions = true
            };

            changeCell = _dg.Columns[7].GetCellContent(row).Parent as DataGridCell;
            if (changeCell != null)
                changeCell.SetBinding(DataGridCell.BackgroundProperty, bindingVendor);
        }
        void SortGridColumns()
        {
            Microsoft.LightSwitch.Threading.Dispatchers.Main.BeginInvoke(() =>
            {
                PagedCollectionView view = new PagedCollectionView(_dg.ItemsSource);
                using (view.DeferRefresh())
                {
                    // What do you want to group by
                    //view.GroupDescriptions.Add(new PropertyGroupDescription("Depth"));

                    // What do you want to order by
                    view.SortDescriptions.Add(new SortDescription("Level", ListSortDirection.Ascending));
                    view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                    
                }
                //view.MoveCurrentToFirst();
                _dg.ItemsSource = view;
            });
        } // selected item event doesn't fire and position doesn't change

        partial void JobDetail_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            this.DisplayName = "Job " + this.Job.JobNumber;
            this.FindControl("Job_NextPoNumber").IsReadOnly = !this.Application.User.HasPermission(Permissions.OverrideNextPONo);
        }

        partial void JobMasterBOMItemsSearch_Loaded(bool succeeded)
        {
            // disable vendor dropdown and status if a POLine exists or if Product.CanHaveBOM
            foreach (JobMasterBOMItem jmbi in JobMasterBOMItemsSearch)
            {
                if (jmbi.POLineId.HasValue || jmbi.Product.ProductType.CanHaveBOM)
                {
                    this.FindControlInCollection("Vendor", jmbi).IsEnabled = false;
                    this.FindControlInCollection("PurchaseOrderStatus", jmbi).IsEnabled = false;
                }
            }

            if (this.JobMasterBOMs.SelectedItem != null)
            {
                var deepestBomItem = this.JobMasterBOMs.SelectedItem.JobMasterBOMItemQuery.OrderByDescending(j => j.Depth).FirstOrDefault();
                if (deepestBomItem != null)
                {
                    _maxDepth = deepestBomItem.Depth;
                }
            }
            _loadedOnce = true;
        }

        partial void JobDetail_Activated()
        {
             //Refresh the JMBI grid
            Debug.WriteLine("JobDetail_Activated");
            if (!_loadedOnce)
            {
                this.JobMasterBOMItemsSearch.Load();
                this.PurchaseOrders.Load();
            }           
        }

        partial void Job_Changed()
        {
            if (this.Job.Details.EntityState == EntityState.Unchanged || this.Job.Details.EntityState == EntityState.Deleted
                || this.Job.Details.EntityState == EntityState.Discarded) return;
            this.SetDisplayNameFromEntity(this.Job);
        }
        
        partial void JobDetail_Saved()
        {
            this.SetDisplayNameFromEntity(this.Job);
            _validatedOnce = false;
        }

        #region ##### BUTTON METHODS #####

        partial void BOMReport_CanExecute(ref bool result)
        {
            result = this.JobMasterBOMs.SelectedItem != null;
        }
        partial void BOMReport_Execute()
        {
            this.DataWorkspace.ApplicationData.SaveChanges();
            Application.ShowJobDetailReport(this.Job.Id);
        }
        partial void CancelChanges_Execute()
        {
            // Write your code here.
            foreach (JobMasterBOMItem j in this.DataWorkspace.ApplicationData.Details.GetChanges().OfType<JobMasterBOMItem>())
            {
                j.Details.DiscardChanges();
            }
        }
        private void CreateSinglePOLine(JobMasterBOMItem jmbi)        
        {
            // USED IN THE ROUTINE TO CREATE REQUISITIONS
            //
            if (jmbi.POLineId == null &&
                jmbi.Product.ProductType.CanHaveBOM == false &&
                jmbi.PurchaseOrderStatus.IsLocked == false &&
                jmbi.Vendor != null)
            {
                //  Find the first PO in the POs for this job that is for this vendor and not at a locked status 
                PurchaseOrder po = jmbi.JobMasterBOM.Job.PurchaseOrders
                    .Where(p =>
                        p.PurchaseOrderStatus.IsLocked == false &&
                        p.Company == jmbi.Vendor)
                    .OrderBy(p => p.PurchaseOrderStatus.Seq)
                    .FirstOrDefault();

                // If an existing PO was not found, Create a new one
                if (po == null)
                {
                    // TODO: Use a temporary data workspace and TEST    %%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                    po = new PurchaseOrder();
                    PurchaseOrderLine poLine = new PurchaseOrderLine();

                    // create the PO, Job#, Vendor, etc
                    po.Company = jmbi.Vendor;
                    po.Job = jmbi.JobMasterBOM.Job;
                    po.DateDue = DateTime.Today.AddDays(jmbi.Vendor.DefaultLeadDays.GetValueOrDefault(0));
                    po.PurchaseOrderStatus = this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.Skip(1).FirstOrDefault();
                    po.Number = string.Format("{0}-{1}",
                        jmbi.JobMasterBOM.Job.JobNumber,
                        jmbi.JobMasterBOM.Job.NextPoNumber);   // remember that the next po# will increment when inserted through the save pipeline

                    // increment the Job's next PO#
                    jmbi.JobMasterBOM.Job.NextPoNumber++;

                    // add the PO Line info
                    poLine.PurchaseOrder = po;
                    poLine.JobMasterBOMItemId = jmbi.Id;
                    poLine.OrderQty = jmbi.ExtendedQty.GetValueOrDefault(0);
                    poLine.Price = jmbi.UnitCost.GetValueOrDefault(0);
                    poLine.Product = jmbi.Product;
                }
                else  // and existing PO WAS found just add lines to it.
                {
                    // add the PO Line info
                    PurchaseOrderLine poLine = new PurchaseOrderLine();
                    poLine.PurchaseOrder = po;
                    poLine.JobMasterBOMItemId = jmbi.Id;
                    poLine.OrderQty = jmbi.ExtendedQty.GetValueOrDefault(0);
                    poLine.Price = jmbi.UnitCost.GetValueOrDefault(0);
                    poLine.Product = jmbi.Product;
                    // if the PO status was still the first status (created), set it to the next (requisition)
                    PurchaseOrderStatus reqStatus = this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.Skip(1).First();
                    if (po.PurchaseOrderStatus.Id == this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.FirstOrDefault().Id)
                    {
                        po.PurchaseOrderStatus = reqStatus;
                    }
                    else
                    {
                        jmbi.PurchaseOrderStatus = po.PurchaseOrderStatus;
                    }
                }
            }
        }
        partial void CreatePO_Execute()                 // Called from the Create/Open PO grid command to CREATE or OPEN a PO
        {
            // if there is a PO Line already, open it
            //Debug.WriteLine("%%%%%%%%%%  CreatePO_Execute selectedItem: " + JobMasterBOMItemsSearch.SelectedItem.Id.ToString());
            if (this.JobMasterBOMItemsSearch.SelectedItem.POLineId.HasValue)
            {
                PurchaseOrderLine pol = this.DataWorkspace.ApplicationData.PurchaseOrderLines.Where(p => p.Id == JobMasterBOMItemsSearch.SelectedItem.POLineId).FirstOrDefault();
                if (pol != null)
                {
                    PurchaseOrder po = pol.PurchaseOrder;
                    _loadedOnce = false;
                    if (po!=null) Application.ShowPurchaseOrderDetail(po.Id);
                }
            }
            else
            {               
                // if there is no PO line, and it is a purchased part (cannot have BOM), and it is not already closed
                if (!this.JobMasterBOMItemsSearch.SelectedItem.Product.ProductType.CanHaveBOM)
                {
                    // If there is no vendor, indicate this
                    if (this.JobMasterBOMItemsSearch.SelectedItem.Vendor == null)
                    {
                        this.ShowMessageBox("Please select a vendor first", "Cannot generate a PO...", MessageBoxOption.Ok);
                    }
                    else
                    {
                        // if already marked closed, quit
                        if (this.JobMasterBOMItemsSearch.SelectedItem.PurchaseOrderStatus.IsClosed)
                        {
                            this.ShowMessageBox("Status is considered closed. Cannot create a PO for this line.", "Cannot generate a PO...", MessageBoxOption.Ok);
                            return;
                        }
                        
                        // otherwise, prompt to create a PO
                        if (this.DataWorkspace.ApplicationData.Details.HasChanges)
                        {
                            this.DataWorkspace.ApplicationData.SaveChanges();
                        }
                        if (this.ShowMessageBox("This item is not on a PO.  Would you like to generate one?",
                        "Create PO Line?", MessageBoxOption.YesNo) == System.Windows.MessageBoxResult.Yes)
                        {
                            this.Save();
                            CreateSinglePOLine(this.JobMasterBOMItemsSearch.SelectedItem);     // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%  TODO do this in a temp DW and savechanges in the routine / CreateSinglePOLine
                            this.Save();
                            
                            // open the PO
                            // the line below was remmed out to prevent it from opening
                            // CreatePO_Execute();
                        }
                    }
                }
                else
                {
                    // product is not a purchased part - warn
                    this.ShowMessageBox(string.Format("{0}-Type parts cannot be purchased.  Only the subcomponents may be ordered. \nId: {1}", this.JobMasterBOMItemsSearch.SelectedItem.Product.ProductType.Code, this.JobMasterBOMItemsSearch.SelectedItem.Id));
                }
                
            }

        }
        partial void CreateRequisitions_Execute()
        {
            // Create PO Requisitions for all items that dont have a po line yet, and have a default vendor.
            foreach (JobMasterBOMItem j in this.JobMasterBOMItemsSearch.Where(jmbi => (jmbi.POLineId.HasValue == false && jmbi.Vendor != null)))
            {
                CreateSinglePOLine(j);
            }
            this.Save();
        }

        partial void ExplodeBOM_CanExecute(ref bool result)
        {
            // Only allow if a product exists
            result = this.JobMasterBOMs.SelectedItem != null;
        }
        partial void ExplodeBOM_Execute()
        {
            Debug.WriteLine("%%%%%%%%%%%%%%%   Begin explode BOM Execute   %%%%%%%%%%%%%%%%%%%%%%");
            bool reExploding = this.JobMasterBOMs.SelectedItem.JobMasterBOMItem.Count() > 0;
            this.DataWorkspace.ApplicationData.SaveChanges();

            using (var tempDataWorkspace = new DataWorkspace())
            {
                int jmbId = this.JobMasterBOMs.SelectedItem.Id;

                Explosion newExplosion;

                if (reExploding && Application.User.HasPermission(Permissions.ProcessDuplicatesFirst))
                {
                    // process duplicates first if set in security
                    this.ShowMessageBox("Processing duplicates first.  This can be turned off by removing the permission.");
                    newExplosion = tempDataWorkspace.ApplicationData.Explosions.AddNew();
                    newExplosion.JobMasterBomId = jmbId;
                    newExplosion.Action = 2;
                    try
                    {
                        tempDataWorkspace.ApplicationData.SaveChanges();
                        newExplosion.Delete();
                        tempDataWorkspace.ApplicationData.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        this.ShowMessageBox("FAILED TO PROCESS DUPLICATES..." + ex.ToString());
                    }
                }


                //build bom
                newExplosion = tempDataWorkspace.ApplicationData.Explosions.AddNew();
                newExplosion.JobMasterBomId = jmbId;
                newExplosion.Action = 1;
                try
                {
                    tempDataWorkspace.ApplicationData.SaveChanges();
                    newExplosion.Delete();
                    tempDataWorkspace.ApplicationData.SaveChanges();
                }
                catch (Exception ex)
                {
                    this.ShowMessageBox("FAILED TO EXPLODE..." + ex.ToString());
                }

                if (reExploding)
                {
                    // process duplicates
                    newExplosion = tempDataWorkspace.ApplicationData.Explosions.AddNew();
                    newExplosion.JobMasterBomId = jmbId;
                    newExplosion.Action = 2;
                    try
                    {
                        tempDataWorkspace.ApplicationData.SaveChanges();
                        newExplosion.Delete();
                        tempDataWorkspace.ApplicationData.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        this.ShowMessageBox("FAILED TO PROCESS DUPLICATES..." + ex.ToString());
                    }

                    //process deletions
                    newExplosion = tempDataWorkspace.ApplicationData.Explosions.AddNew();
                    newExplosion.JobMasterBomId = jmbId;
                    newExplosion.Action = 3;
                    try
                    {
                        tempDataWorkspace.ApplicationData.SaveChanges();
                        newExplosion.Delete();
                        tempDataWorkspace.ApplicationData.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        this.ShowMessageBox("FAILED TO PROCESS DELETIONS..." + ex.ToString());
                    }
                }

            }

            this.JobMasterBOMItemsSearch.Refresh();

        }

        partial void FilterLevel1_Execute()
        {
            ParamOptDepth = 1;
        }
        partial void FilterLevelOff_Execute()
        {
            ParamOptDepth = null;
        }
        partial void FilterMoreDetail_Execute()
        {
            // Increase depth filter if not already at max depth
            if (ParamOptDepth < _maxDepth)
            {
                ParamOptDepth++;
            }
        }
        partial void FilterLessDetail_Execute()
        {
            // Decrease depth filter if not already at 1
            if (!ParamOptDepth.HasValue)
            {
                ParamOptDepth = _maxDepth;
            }
            if (ParamOptDepth > 1)
            {
                ParamOptDepth--;
            }
        }

        partial void JobDetailMiscReport_Execute()
        {
            this.DataWorkspace.ApplicationData.SaveChanges();
            Application.ShowJobDetailReportMisc(this.Job.Id);
        }

        partial void OpenSelectedJMB_Execute()
        {
            Application.ShowProductDetail(this.JobMasterBOMs.SelectedItem.Product.Id);
        }
        partial void OpenSelectedProduct_Execute()
        {
            this.Application.ShowProductDetail(this.JobMasterBOMItemsSearch.SelectedItem.Product.Id);
        }
        partial void PreviewJobCard_Execute()
        {
            this.DataWorkspace.ApplicationData.SaveChanges();
            Application.ShowJobDetailCardReport(this.Job.Id);
        }
        
        partial void PurchaseOrdersAddNew_Execute()
        {
            Application.ShowPurchaseOrderAdd(this.Job.Id);
        }
        
        #endregion

        partial void JobMasterBOMItemsSearch_Validate(ScreenValidationResultsBuilder results)
        {
            // VALIDATE JMBI DELETIONS
            if (this.DataWorkspace.ApplicationData.Details.HasChanges)
            {
                EntityChangeSet changeset =
                    this.DataWorkspace.ApplicationData.Details.GetChanges();
                foreach (JobMasterBOMItem jmbi in changeset.DeletedEntities.OfType<JobMasterBOMItem>())
                {
                    if (!jmbi.IsDeletedInBOM)
                    {
                        // If the JMBI has Child Items, Deny the deletion.  Note there is no collection.
                        string childPathSearch = jmbi.IdPath + ",";   // includes the ending comma
                        bool childrenExist = jmbi.JobMasterBOM.JobMasterBOMItemQuery.Where(j => j.IdPath.StartsWith(childPathSearch)).FirstOrDefault() != null;
                        //bool duplicateParentsExist = jmbi.JobMasterBOM.JobMasterBOMItemQuery.Where(j => j.IdPath.Equals(jmbi.IdPath)).Execute().Count() >1 ;
                        int duplicateParentCount = jmbi.JobMasterBOM.JobMasterBOMItemQuery.Where(j => j.IdPath.Equals(jmbi.IdPath)).Execute().Count();

                        if (childrenExist && !jmbi.Processed && duplicateParentCount == 1) // in Application.ProcessDuplicateJMBI, we mark the item Processed to allow any deletions done by that procedure
                        {
                            results.AddScreenError(string.Format("Level {0}: You must delete all child items before deleting the parent", jmbi.Level));
                        }

                        // If there is a POLineId that is Locked or Closed, deny the deletion and discard it.
                        // Before deleting, user must delete the PO Line
                        if (jmbi.POLineId.HasValue)
                        {
                            if (jmbi.PurchaseOrderStatus.IsClosed || jmbi.PurchaseOrderStatus.IsLocked)
                            {
                                jmbi.Details.DiscardChanges();
                                results.AddScreenResult(string.Format("Level {0}: Deleting items with a PO Line at a closed or ordered status is not allowed.", jmbi.Level),
                                    ValidationSeverity.Error);
                            }
                            else
                            {
                                // If there is a POLineId and JMBI is NOT Locked or Closed (created or requisition)
                                // delete the PO Line
                                if (!_validatedOnce)
                                {
                                    string messageText = string.Format("JMB {0}, Part {1} at level {2} was deleted. PO Line id {3} was cleared.",
                                            jmbi.JobMasterBOM.Product.PartNumber, jmbi.Product.PartNumber, jmbi.Level, jmbi.POLineId);

                                    //PurchaseOrderLine pol = this.DataWorkspace.ApplicationData.PurchaseOrderLines_SingleOrDefault(jmbi.POLineId);
                                    PurchaseOrderLine pol = this.DataWorkspace.ApplicationData.PurchaseOrderLines.Where(l => l.Id == jmbi.POLineId).FirstOrDefault();

                                    if (pol != null)  // make sure the po line entity exists
                                    {
                                        pol.PurchaseOrder.InternalNote = messageText + "..." + pol.PurchaseOrder.InternalNote;
                                        pol.Delete();
                                    }

                                    Application.WriteMessage(jmbi.JobMasterBOM.Job, "Deleted JMBI", messageText);
                                    _validatedOnce = true;
                                    //this.Save(); // prevents double validation
                                }
                            }
                        }
                        else
                        {
                            // If PO LineId is not populated, regardless of status, allow the deletion.
                            // and update PARENT statuses
                            UpdateJMBIParentStatus(jmbi);
                        } // End if (jmbi.POLineId.HasValue)
                    } 
                }// foreach
            }
        }

        private void UpdateJMBIParentStatus(JobMasterBOMItem jobMasterBomItem)
        {
            // update the parent PO status to the lowest of the child PO statuses
            
            if (!string.IsNullOrEmpty(jobMasterBomItem.IdPath))
            {
                int lastCommaPos;
                lastCommaPos = jobMasterBomItem.IdPath.LastIndexOf(",");

                if (lastCommaPos != -1) // verify that there is a comma  
                {                    
                    string parentIdPath = jobMasterBomItem.IdPath.Substring(0, lastCommaPos);  // strips comma
                    string childPathSearch = jobMasterBomItem.IdPath.Substring(0, lastCommaPos) + ",";   // includes the ending comma

                    JobMasterBOMItem parentJMBI = jobMasterBomItem.JobMasterBOM.JobMasterBOMItemQuery
                        .Where(j =>j.IdPath.Equals(parentIdPath))
                        .FirstOrDefault();

                    if (parentJMBI != null)
                    {
                        JobMasterBOMItem lowestJMBI = jobMasterBomItem.JobMasterBOM.JobMasterBOMItemQuery
                            .Where(j => j.IdPath != jobMasterBomItem.IdPath && j.IdPath.StartsWith(childPathSearch))
                            .OrderBy(j => j.PurchaseOrderStatus.Seq).FirstOrDefault();
                        if (lowestJMBI != null)
                        {
                            PurchaseOrderStatus lowestPOStatus = lowestJMBI.PurchaseOrderStatus;
                            parentJMBI.PurchaseOrderStatus = lowestPOStatus;
                        }
                    }

                }
            }
        }
        
        partial void JobMasterBOMs_SelectionChanged()
        {
            if (this.JobMasterBOMs.SelectedItem != null && this.JobMasterBOMs.SelectedItem.Product != null)
            {
                if (ParamOptDepth != null || ParamOptLevel !=null)
                {
                    // this will trigger a reload so only do it if needed
                    ParamOptDepth = null;
                    ParamOptLevel = null;
                }
                
            } 
          
        }

        partial void ResolveConflict_CanExecute(ref bool result)
        {
            // only show if there is a conflict
            if (this.JobMasterBOMItemsSearch.SelectedItem == null)
            {
                result = false;
            }
            else
            {
                JobMasterBOMItem jobMasterBOMItem = this.JobMasterBOMItemsSearch.SelectedItem;
                result = (jobMasterBOMItem.ExtendedQtyRegenerated != null
                    || jobMasterBOMItem.PerAssemblyQtyRegenerated != null
                    || jobMasterBOMItem.IsDeletedInBOM);
            }

        }

        partial void ResolveConflict_Execute()
        {
            JobMasterBOMItem j = this.JobMasterBOMItemsSearch.SelectedItem;
            string title = string.Empty;
            string q1 = string.Empty;
            string q2 = string.Empty;
            string q3 = string.Empty;
            System.Windows.MessageBoxResult answer;

            #region DELETED
            if (j.IsDeletedInBOM)
            {
                // first ask if you want to delete the item from this job
                title = "Resolve item deleted from BOM structure...";
                q1 = string.Format("Click YES to delete {0} from this list AND it's related PO line.",j.Product.PartNumber);
                q2 = "Click NO to keep item and clear the note.";
                q3 = "Click CANCEL to leave it alone and decide later";
                
                answer = this.ShowMessageBox(string.Format("{0}\n{1}\n{2}", q1, q2, q3), title,
                        MessageBoxOption.YesNoCancel);

                switch (answer)
                {
                    case System.Windows.MessageBoxResult.No:            // the answer was NO - keep the line
                        j.Note = string.Empty;
                        // if there were also quantity discrepancies from before, clear them
                        // if they were a discrepancy but now are not, clear them
                        if (j.PerAssemblyQtyRegenerated != null) j.PerAssemblyQtyRegenerated = null;
                        if (j.ExtendedQtyRegenerated != null) j.ExtendedQtyRegenerated = null;
                        break;
                    case System.Windows.MessageBoxResult.Yes:           // the answer was YES - delete the line
                        // if there is a PO line, delete it
                        if (j.POLineId.HasValue)
                        {
                            PurchaseOrderLine poLine = this.DataWorkspace.ApplicationData.PurchaseOrderLines.Where(p => p.Id == j.POLineId).FirstOrDefault();
                            if (poLine != null)
                            {
                                poLine.Delete();                                
                            }
                        }
                        j.Delete();
                        this.DataWorkspace.ApplicationData.SaveChanges();
                        break;
                    default:
                        break;
                }
            }
            #endregion

            #region QUANTITY CONFLICT
            if (j.PerAssemblyQtyRegenerated != null || j.ExtendedQtyRegenerated != null)
            {
                // ask: Accept the revised Quantities? (the note needs to indicate CONFLICT: Revised BOM QtyPer=2. Revised Extended Qty=3.)
                // YES to do this: 
                //  if there is a PO, update the jmbi and PO line quantity
                //   else just update the jmbi.
                // NO to leave the quantity at __ and clear the note.
                // CANCEL to leave thhe conflict and resolve it later.

                title = "Resolve quantity BOM quantity difference...";
                q1 = "Click YES to update the ";
                if (j.PerAssemblyQty!=j.PerAssemblyQtyRegenerated)
                    q1 += string.Format("Qty Per Assy to {0} and ", j.PerAssemblyQtyRegenerated);
                if (j.ExtendedQty!=j.ExtendedQtyRegenerated.GetValueOrDefault())
                    q1 += string.Format("Extended Qty to {0}.", j.ExtendedQtyRegenerated.GetValueOrDefault());

                if (j.POLineId != null)
                {
                    q2 = "The PO quantities will also be updated and PO status set to REVISED.";

                    q3 = "Click NO to leave quantities as they are and clear the conflict note.\n\n";
                    q3 += "Note: to increase quantity of an item already purchased and received:\n";
                    q3 += "1) add this same component again to the parent part\n";
                    q3 += "2) set the Per Assy Qty to the balance needed. \n";
                    q3 += "3) Re-explode the BOM and create a new PO for the extras.";
                }
                else
                {
                    q2 = string.Empty;
                    q3 = string.Empty;
                }
                
                answer = this.ShowMessageBox(string.Format("{0}\n{1}\n\n{2}", q1, q2, q3), title,
                        MessageBoxOption.YesNoCancel);

                switch (answer)
                {
                    case System.Windows.MessageBoxResult.Cancel:
                        return;

                    case System.Windows.MessageBoxResult.No:
                        j.PerAssemblyQtyRegenerated = null;
                        j.ExtendedQtyRegenerated = null;
                        j.Note = string.Empty;
                        break;

                    case System.Windows.MessageBoxResult.Yes:
                        j.PerAssemblyQty = j.PerAssemblyQtyRegenerated;
                        j.ExtendedQty = j.ExtendedQtyRegenerated;
                        j.PerAssemblyQtyRegenerated = null;
                        j.ExtendedQtyRegenerated = null;
                        j.Note = string.Empty;

                        if (j.POLineId.HasValue)
                        {
                            // update po line quantities
                            PurchaseOrderLine pl = this.DataWorkspace.ApplicationData.PurchaseOrderLines.Where(p => p.Id == j.POLineId).FirstOrDefault();
                            if (pl!=null)
                            {
                                // PO line exists so update it
                                pl.OrderQty = j.ExtendedQty.GetValueOrDefault();
                                PurchaseOrderStatus pos = this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.Where(s => s.IsRevised == true).FirstOrDefault();
                                if (pos != null)
                                {
                                    pl.PurchaseOrder.PurchaseOrderStatus = pos;
                                }                               
                            }
                            else
                            {
                                // somehow the po line is not valid or was deleted - so just clear it.
                                j.POLineId = null;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            #endregion
            // save changes
            // todo: add try catch and check for validation exceptions
            this.DataWorkspace.ApplicationData.SaveChanges();
        }

        partial void JobMasterBOMItemsSearch_SelectionChanged()
        {
            if (this.JobMasterBOMItemsSearch.SelectedItem!=null)
            {
                SelectedSystemNote = this.JobMasterBOMItemsSearch.SelectedItem.Note;
            }
        }

        partial void ClearSystemNote_CanExecute(ref bool result)
        {
            if (this.JobMasterBOMItemsSearch.SelectedItem == null)
            {
                result = false;
            }
            else
            {
                result = !string.IsNullOrEmpty(this.JobMasterBOMItemsSearch.SelectedItem.Note);
            }            
        }

        partial void ClearSystemNote_Execute()
        {
            this.JobMasterBOMItemsSearch.SelectedItem.Note = string.Empty;
        }

        partial void ProcessDuplicates_Execute()
        {

            using (var tempDataWorkspace = new DataWorkspace())
            {
                int jmbId = this.JobMasterBOMs.SelectedItem.Id;

                Explosion newExplosion;
                    newExplosion = tempDataWorkspace.ApplicationData.Explosions.AddNew();
                    newExplosion.JobMasterBomId = jmbId;
                    newExplosion.Action = 2;
                    try
                    {
                        tempDataWorkspace.ApplicationData.SaveChanges();
                        newExplosion.Delete();
                        tempDataWorkspace.ApplicationData.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        this.ShowMessageBox("FAILED TO PROCESS DUPLICATES..." + ex.ToString());
                    }
            }

        }


    }
}