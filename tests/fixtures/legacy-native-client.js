// Compatibility fixture: response handling shipped in companion 1.3.5.
globalThis.legacySendLink = async value => {
  const response = await browser.runtime.sendNativeMessage('com.vifree', { version: 1, type: 'open-link', url: new URL(value).href });
  if (!response?.ok) {
    const error = new Error(response?.message ?? 'Open Vydrop and enable browser integration first.');
    if (response?.code === 'busy') error.code = 'busy';
    throw error;
  }
  return response;
};
