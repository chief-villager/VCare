using VCare.Api.Services;
using Xunit;

namespace VCare.Api.Tests;

// What a stored file is served as is decided by its extension, from a short
// list, never by the content type the uploading client claims.
public class UploadPolicyTests
{
    [Theory]
    [InlineData("hospital-letter.pdf", "application/pdf")]
    [InlineData("Consent Form.PDF", "application/pdf")]
    [InlineData("wound.jpg", "image/jpeg")]
    [InlineData("care-notes.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void A_document_is_stored_as_the_type_its_extension_names(string fileName, string expected)
    {
        var type = UploadPolicy.ContentTypeFor(fileName, UploadPolicy.Documents);

        Assert.True(type.IsSuccess);
        Assert.Equal(expected, type.Value);
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("script.js")]
    [InlineData("setup.exe")]
    [InlineData("no-extension")]
    public void Files_a_browser_would_run_are_refused(string fileName)
    {
        var type = UploadPolicy.ContentTypeFor(fileName, UploadPolicy.Documents);

        Assert.Equal("Documents.FileTypeNotAllowed", type.Error.Code);
    }

    [Fact]
    public void A_profile_image_must_be_an_image()
    {
        Assert.True(UploadPolicy.ContentTypeFor("me.png", UploadPolicy.ProfileImages).IsSuccess);
        Assert.Equal("Documents.FileTypeNotAllowed", UploadPolicy.ContentTypeFor("me.pdf", UploadPolicy.ProfileImages).Error.Code);
    }

    [Fact]
    public void A_download_is_named_after_the_file_that_was_uploaded()
    {
        Assert.Equal("hospital-letter.pdf",
            UploadPolicy.DownloadName("home-a/documents/1f2e3d4c5b6a79881f2e3d4c5b6a7988_hospital-letter.pdf"));
    }
}
