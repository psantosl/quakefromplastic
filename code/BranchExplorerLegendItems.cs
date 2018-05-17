using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.Layout.Drawing.Colors;
using Codice.CM.Common;
using Codice.I3;

namespace Codice.CM.Client.Branch
{
    public enum LegendItemType
    {
        Branch,
        Changeset,
        HeadChangeset,
        WorkspaceChangeset,
        CheckoutChangeset,
        RemoteChangeset,
        Label,
        ChangesetLink,
        RelevantChangesetLink,
        MergeLink,
        CherryPickLink,
        SubtractiveLink,
        InProgressMergeLink,
        InProgressCherryPick,
        InProgressSubtractive,
    }

    public abstract class LegendItem
    {
        public static LegendItem Create(LegendItemType type)
        {
            switch (type)
            {
                case LegendItemType.Branch:
                    return new BranchLegendItem();
                case LegendItemType.Changeset:
                    return new ChangesetLegendItem();
                case LegendItemType.HeadChangeset:
                    return new HeadChangesetLegendItem();
                case LegendItemType.WorkspaceChangeset:
                    return new WorkspaceChangesetLegendItem();
                case LegendItemType.CheckoutChangeset:
                    return new CheckoutChangesetLegendItem();
                case LegendItemType.RemoteChangeset:
                    return new RemoteChangesetLegendItem();
                case LegendItemType.Label:
                    return new LabelLegendItem();
                case LegendItemType.ChangesetLink:
                    return new ChangesetLinkLegendItem();
                case LegendItemType.RelevantChangesetLink:
                    return new RelevantChangesetLegendItem();
                case LegendItemType.MergeLink:
                    return new MergeLinkLegendItem();
                case LegendItemType.CherryPickLink:
                    return new CherryPickLegendItem();
                case LegendItemType.SubtractiveLink:
                    return new SubtractiveLinkLegendItem();
                case LegendItemType.InProgressMergeLink:
                    return new InProgressMergeLinkLegendItem();
                case LegendItemType.InProgressCherryPick:
                    return new InProgressCherryPickLinkLegendItem();
                case LegendItemType.InProgressSubtractive:
                    return new InProgressSubtractiveLinkLegendItem();
            }

            return new BranchLegendItem();
        }

        public void Dispose()
        {
            if (mImage != null)
                mImage.Dispose();
        }

        public Image GetImage(Size size, DrawingManager drawingManager)
        {
            if (mImage != null)
                return mImage;

            mImage = CreateLegendItemImage(size, drawingManager);
            return mImage;
        }

        Image CreateLegendItemImage(Size size, DrawingManager drawingManager)
        {
            Image image = new Bitmap(size.Width, size.Height);

            using (Graphics g = Graphics.FromImage(image))
            using (Matrix mx = new Matrix())
            {
                Rectangle clip = new Rectangle(0, 0, image.Width, image.Height);
                PaintEventArgs args = new PaintEventArgs(g, clip);

                mx.Scale(Dpi.DpiXFactor, Dpi.DpiYFactor);
                g.Transform = mx;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int margin = 2;

                Rectangle itemRectangle = new Rectangle(
                    margin, margin,
                    Dpi.Unadjust(image.Width) - margin,
                    Dpi.Unadjust(image.Height) - margin);

                DrawLegendItem(args, itemRectangle, drawingManager);
            }

            return image;
        }

        public abstract string GetCaption();
        public abstract void DrawLegendItem(
            PaintEventArgs e, Rectangle bounds, DrawingManager manager);

        Image mImage;
    }

    internal class BranchLegendItem : LegendItem
    {
        public BranchLegendItem() : 
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("BRANCH");
        }

        public override void DrawLegendItem(
            PaintEventArgs e, Rectangle bounds, DrawingManager manager)
        {
            BranchDraw brDraw = new BranchDraw(0);

            Rectangle branchBounds = 
                new Rectangle(bounds.X, bounds.Y, 2 * (bounds.Width / 3),
                BrExDrawProperties.ChangesetRadius * 2);

            brDraw.Bounds = branchBounds;
            brDraw.DrawingPath = BranchRectangle.Create(
                branchBounds, new SubBranchContainerDraw[0]);

            manager.Style.DrawBranch(e, brDraw);
        }
    }


    internal class ChangesetLegendItem : LegendItem
    {
        public ChangesetLegendItem() : 
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("CHANGESET");
        }

        protected virtual ChangesetDraw GetChangesetDraw(
            Rectangle bounds, DrawingManager manager)
        {
            Rectangle csetBounds = new Rectangle(
                bounds.X, bounds.Y,
                BrExDrawProperties.ChangesetDrawingWidth,
                BrExDrawProperties.ChangesetDrawingHeight);

            return Create(csetBounds, BrExDrawColors.Name.MainColor);
        }

        public override void DrawLegendItem(
            PaintEventArgs e, Rectangle bounds, DrawingManager manager)
        {
            ChangesetDraw cset = GetChangesetDraw(bounds, manager);

            manager.Style.DrawChangeset(e, cset);
        }

        static ChangesetDraw Create(Rectangle csetBounds, BrExDrawColors.Name color)
        {
            var result = new ChangesetDraw();
            result.DrawingPath = ChangesetDrawingPath.Create(csetBounds);
            result.Bounds = csetBounds;
            result.Tag = BrExChangeset.CreateEmptyChangeset();
            result.Color = BrExColors.GetColor(
                BrExDrawColors.GetColor(color));

            return result;
        }
    }

    internal class HeadChangesetLegendItem : ChangesetLegendItem
    {
        public HeadChangesetLegendItem () : 
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("HEAD_CHANGESET");
        }

        protected override ChangesetDraw GetChangesetDraw(
            Rectangle bounds, DrawingManager manager)
        {
            ChangesetDraw cset = base.GetChangesetDraw (bounds, manager);
            cset.IsHead = true;
            return cset;
        }
     }

    internal class WorkspaceChangesetLegendItem : ChangesetLegendItem
    {
        public WorkspaceChangesetLegendItem () : 
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("WORKING_CHANGESET");
        }

        protected override ChangesetDraw GetChangesetDraw(
            Rectangle bounds, DrawingManager manager)
        {
            ChangesetDraw cset = base.GetChangesetDraw (bounds, manager);
            cset.IsWorkspaceChangeset = true;
            return cset;
        }
    }

    internal class CheckoutChangesetLegendItem : ChangesetLegendItem
    {
        public CheckoutChangesetLegendItem() :
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("CHECKOUT_CHANGESET");
        }

        protected override ChangesetDraw GetChangesetDraw(
            Rectangle bounds, DrawingManager manager)
        {
            ChangesetDraw cset = base.GetChangesetDraw(bounds, manager);
            cset.IsCheckoutChangeset = true;
            cset.IsWorkspaceChangeset = true;
            return cset;
        }
    }

    internal class RemoteChangesetLegendItem : ChangesetLegendItem
    {
        public RemoteChangesetLegendItem () :
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("REMOTE_CHANGESET");
        }

        protected override ChangesetDraw GetChangesetDraw(
            Rectangle bounds, DrawingManager manager)
        {
            ChangesetDraw cset = base.GetChangesetDraw (bounds, manager);
            cset.IsRemote = true;
            return cset;
        }
    }

    internal class LabelLegendItem : LegendItem
    {
        public LabelLegendItem() :
        base() { }

        public override string GetCaption()
        {
            return Localization.GetString("LABEL");
        }

        public override void DrawLegendItem(
            PaintEventArgs e, Rectangle bounds, DrawingManager manager)
        {
            LabelDraw label = new LabelDraw();

            Rectangle labelBounds = 
                new Rectangle(
                    bounds.X, 
                    bounds.Y, 
                    bounds.Height, 
                    bounds.Height);

            label.Bounds = labelBounds;
            label.DrawingPath = LabelDrawingPath.Create(labelBounds, 3);

            manager.Style.DrawLabel(e, label);
        }
    }

    internal abstract class LinkLegendItem : LegendItem
    {
        public LinkLegendItem() : 
        base() { }
    
        protected abstract Point GetSourcePoint(Rectangle bounds);
        protected abstract Point GetDestinationPoint(Rectangle bounds);
        protected abstract Pen GetPen(DrawingManager manager);

        public override void DrawLegendItem(
            PaintEventArgs e, Rectangle bounds, DrawingManager manager)
        {
            Point source = GetSourcePoint(bounds);
            Point destination = GetDestinationPoint(bounds);

            Pen pen = GetPen(manager);

            e.Graphics.DrawLine(pen, source, destination);
        }
    }

    internal class ChangesetLinkLegendItem : LinkLegendItem
    {
        public ChangesetLinkLegendItem() : 
        base() { }

        public override string GetCaption()
        {
            return Localization.GetString("CHANGESET_PARENT_LINK");
        }

        protected override Point GetSourcePoint(Rectangle bounds)
        {
            return new Point(bounds.Right - bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }

        protected override Point GetDestinationPoint(Rectangle bounds)
        {
            return new Point(bounds.X, bounds.Y + bounds.Height / 2);
        }

        protected override Pen GetPen(DrawingManager manager)
        {
            return BrExPens.GetParentLinkPen(
                ParentLinkColors.GetColor(
                    manager.Style.DrawingStyle, true));
        }
    }

    internal class RelevantChangesetLegendItem : ChangesetLinkLegendItem
    {
        public RelevantChangesetLegendItem() : 
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("RELEVANT_CHANGESET_PARENT_LINK");
        }

        protected override Pen GetPen(DrawingManager manager)
        {
            return BrExPens.GetRelevantParentLinkPen(
                ParentLinkColors.GetColor(
                    manager.Style.DrawingStyle, true));
        }
    }

    internal class MergeLinkLegendItem : LinkLegendItem
    {
        public MergeLinkLegendItem() : 
        base() { }

        public override string GetCaption()
        {
            return Localization.GetString("MERGE_LINK");
        }

        protected override Point GetSourcePoint(Rectangle bounds)
        {
            return new Point(bounds.X, bounds.Y + bounds.Height / 2);
        }

        protected override Point GetDestinationPoint(Rectangle bounds)
        {
            return new Point(bounds.Right - bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }

        protected override Pen GetPen(DrawingManager manager)
        {
            LinkDraw link = GetSampleLinkDraw();

            return BrExPens.GetMergeLinkPen(
                MergeLinkColors.GetColor(
                    link.MergeType, link.IsSelected));
        }

        protected virtual LinkDraw GetSampleLinkDraw()
        {
            LinkDraw link = new LinkDraw();
            link.MergeType = MergeType.Merge;
            return link;
        }
    }

    internal class CherryPickLegendItem : MergeLinkLegendItem
    {
        public CherryPickLegendItem() : 
        base() { }

        public override string GetCaption()
        {
            return Localization.GetString("CHERRY_PICK_LINK");
        }

        protected override LinkDraw GetSampleLinkDraw()
        {
            LinkDraw result = base.GetSampleLinkDraw ();
            result.MergeType = MergeType.Cherrypicking;
            return result;
        }
    }

    internal class SubtractiveLinkLegendItem : MergeLinkLegendItem
    {
        public SubtractiveLinkLegendItem() :
        base() { }

        public override string GetCaption()
        {
            return Localization.GetString("SUBTRACTIVE_MERGE_LINK");
        }

        protected override LinkDraw GetSampleLinkDraw()
        {
            LinkDraw result = base.GetSampleLinkDraw ();
            result.MergeType = MergeType.CherrypickSubtractive;
            return result;
        }
    }

    internal class InProgressMergeLinkLegendItem : MergeLinkLegendItem
    {
        public InProgressMergeLinkLegendItem() :
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("PENDING_MERGE_LINK");
        }

        protected override LinkDraw GetSampleLinkDraw()
        {
            LinkDraw result = base.GetSampleLinkDraw ();
            result.Pending = true;
            return result;
        }
    }

    internal class InProgressCherryPickLinkLegendItem : CherryPickLegendItem
    {
        public InProgressCherryPickLinkLegendItem() :
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("PENDING_CHERRY_PICK_LINK");
        }

        protected override LinkDraw GetSampleLinkDraw()
        {
            LinkDraw result = base.GetSampleLinkDraw ();
            result.Pending = true;
            return result;
        }
    }

    internal class InProgressSubtractiveLinkLegendItem : SubtractiveLinkLegendItem
    {
        public InProgressSubtractiveLinkLegendItem() :
            base() { }

        public override string GetCaption()
        {
            return Localization.GetString("PENDING_SUBTRACTIVE_MERGE_LINK");
        }

        protected override LinkDraw GetSampleLinkDraw()
        {
            LinkDraw result = base.GetSampleLinkDraw ();
            result.Pending = true;
            return result;
        }
    }
}