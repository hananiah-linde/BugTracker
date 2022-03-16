using System.ComponentModel.DataAnnotations;

namespace BugTracker.Extensions;

public class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly int _maxFileSize;
    public MaxFileSizeAttribute(int maxFileSize)
    {
        _maxFileSize = maxFileSize;
    }


    protected override ValidationResult IsValid(
    object value, ValidationContext validationContext)
    {
        IFormFile file = value as IFormFile;
        if (file != null)
        {
            if (file.Length > _maxFileSize)
            {
                return new ValidationResult(GetErrorMessage());
            }
        }


        return ValidationResult.Success;
    }


    public string GetErrorMessage()
    {
        return $"Maximum allowed file size is { _maxFileSize} bytes.";
    }
}

public class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;
    public AllowedExtensionsAttribute(string[] extensions)
    {
        _extensions = extensions;
    }


    protected override ValidationResult IsValid(
    object value, ValidationContext validationContext)
    {
        IFormFile file = value as IFormFile;
        if (file != null)
        {
            string extension = Path.GetExtension(file.FileName);
            if (!_extensions.Contains(extension.ToLower()))
            {
                return new ValidationResult(GetErrorMessage(extension));
            }
        }


        return ValidationResult.Success;
    }


    public string GetErrorMessage(string ext)
    {
        return $"The file extension {ext} is not allowed!";
    }
}