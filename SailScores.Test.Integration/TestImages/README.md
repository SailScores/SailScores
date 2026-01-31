# OCR Test Images

Place test images in this directory for OCR integration tests.

## Recommended Test Images

To thoroughly test the OCR functionality, add images with:

1. **sample-sail-numbers.jpg** - A general test image with sail numbers
2. **numbers-vertical.jpg** - Vertical column of sail numbers (typical race results board)
3. **numbers-horizontal.jpg** - Horizontal row of sail numbers
4. **mixed-numbers-text.jpg** - Mix of numbers and text (e.g., "USA 123", "K-42")

## Image Requirements

- **Format**: JPEG or PNG
- **Size**: Maximum 4MB
- **Content**: Clear, readable text/numbers
- **Quality**: Good lighting, minimal blur

## Example Sources

You can create test images by:
- Taking photos of actual race results boards
- Creating sample images with text editors (save as JPEG/PNG)
- Using screenshots of race results from sailing websites

## Note

Some initial test images are included in the repository, but it has been added to the .gitignore to prevent further images bloating the repo size.
Each developer should add their own test images locally.
