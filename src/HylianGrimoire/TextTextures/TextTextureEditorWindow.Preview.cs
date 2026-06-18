using System.Drawing;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const int SmallTexturePreviewScale = 5;
    private const int PlaceTitleCardPreviewScale = 4;
    private const int BossTitleCardPreviewScale = 4;
    private const double GameOverPreviewScale = 3.0;
    private const double ContinuePlayingPreviewScale = 4.0;
    private const double PauseHeaderPreviewScale = 2.5;

    private const int DefaultEndTitleWidth = 112;
    private const int DefaultEndTitleHeight = 16;

    private void RefreshPreview()
    {
        if (_updatingControls)
        {
            return;
        }

        if (_romData is null || GetSelectedTarget() is not TextTextureTargetItem item)
        {
            ClearPreviews();
            SetStatus(string.Empty);
            return;
        }

        if (!TryFindRenderFonts(out TextTextureFont primaryFont, out TextTextureFont? secondaryFont, out string missingFontMessage))
        {
            ClearPreviews();
            SetStatus(missingFontMessage);
            return;
        }

        string? pauseTemplateRoot = null;
        if (_selectedTextureKind == TextTextureKind.PauseHeaders && !TryFindPauseHeaderTemplateRoot(out pauseTemplateRoot, out string missingTemplateMessage))
        {
            ClearPreviews();
            SetStatus(missingTemplateMessage);
            return;
        }

        try
        {
            using Bitmap generated = RenderTextTexture(item, primaryFont, secondaryFont, pauseTemplateRoot);
            using Bitmap reference = DecodeReference(_romData.DecompressedRom, item);
            ReplaceLastGenerated(generated, CreatePreviewSourceSignature(_romData.DecompressedRom, item));

            using Bitmap generatedDisplay = CreateDisplayBitmap(generated, item);
            using Bitmap referenceDisplay = CreateDisplayBitmap(reference, item);
            using Bitmap generatedPreview = CreatePreviewBitmap(generatedDisplay);
            using Bitmap referencePreview = CreatePreviewBitmap(referenceDisplay);
            SetPreviewSource(_generatedSlot, generatedPreview, "generated");
            SetPreviewSource(_romSlot, referencePreview, "rom");
            ReferenceLabel.Text = _selectedTextureKind == TextTextureKind.PauseHeaders && _showPauseOriginalColors ? "ROM colors" : "ROM";
            SetStatus($"Previewing {item.StatusLabel}.");
        }
        catch (Exception ex)
        {
            ClearPreviews();
            SetStatus($"Preview failed. {UiOperationExceptionHandler.GetDisplayMessage("Text texture preview failed", ex)}");
        }
    }
}
