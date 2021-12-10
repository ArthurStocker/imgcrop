# Image crop PoC

Crop a QR-Code from a file

## Prerequisits

CH-QR-Bill Style  Guide
<https://www.paymentstandards.ch/dam/downloads/style-guide-de.pdf>

CH-QR-Bill Validator
<https://www.swiss-qr-invoice.org/validator/>

CH-QR-Bill Validator Issues (30 Testc-Cases - valid und invalid ones)
<https://github.com/swico/qr-bill>

Net-Vips image library (install with NuGet)
<https://github.com/kleisauke/net-vips>

libvips pre compiled
<https://github.com/libvips/libvips>

Code Project QRLobrary
<https://www.codeproject.com/Articles/1250071/QR-Code-Encoder-and-Decoder-NET-Framework-Standard>

## Installation

 1. Instell libvips
 2. Install NetVips
 3. Modify QRDecoderLibrary to allow custom signatur precision
 4. Run Test Cases from "CH-QR-Bill Validator Issues"

## Side Note

This Poc just locates the QR Code on the page and reade it.
The content after decoding the QR is not validated. 
This PoC just trys to read the QR from different File-Types (PDF, JPEG, PNG), single- and multi-page documents
