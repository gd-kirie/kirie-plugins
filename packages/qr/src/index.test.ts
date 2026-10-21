import type { KirieEventaContext } from "@gd-kirie/ipc-eventa";
import { createContext, defineEventa } from "@moeru/eventa";
import { expect, it } from "vitest";

import { createQrClient } from "./index";

interface Request {
  content: Record<string, unknown>;
  invokeId: string;
}

it("generates PNG bytes through the borrowed context with explicit defaults", async () => {
  const context = createContext() as KirieEventaContext;
  const client = createQrClient(context);
  const png = new Uint8Array([137, 80, 78, 71, 0, 127, 128, 255]);
  context.on(defineEventa<Request>("kirie:qr:generate-image-send"), ({ body }) => {
    expect(body?.content).toEqual({
      text: "Kirie 世界",
      size: 512,
      foreground: "#000000",
      background: "#ffffff",
    });
    context.emit(defineEventa(`kirie:qr:generate-image-receive-${body!.invokeId}`), {
      content: { pngBase64: "iVBORwB/gP8=" },
      invokeId: body!.invokeId,
    });
  });
  try {
    await expect(client.generateImage({ text: "Kirie 世界" })).resolves.toEqual(png);
  } finally {
    context.abort();
  }
});

it("encodes a large PNG request without changing its bytes", async () => {
  const context = createContext() as KirieEventaContext;
  const client = createQrClient(context);
  const png = Uint8Array.from({ length: 1_000_000 }, (_, index) => index % 256);
  let encoded: string | undefined;
  context.on(defineEventa<Request>("kirie:qr:scan-image-send"), ({ body }) => {
    encoded = body!.content.pngBase64 as string;
    context.emit(defineEventa(`kirie:qr:scan-image-receive-${body!.invokeId}`), {
      content: { text: null },
      invokeId: body!.invokeId,
    });
  });
  try {
    await client.scanImage(png);
    expect(encoded).toBeDefined();
    const received = Uint8Array.from(atob(encoded!), (character) => character.charCodeAt(0));
    expect(received).toEqual(png);
  } finally {
    context.abort();
  }
});
