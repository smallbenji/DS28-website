
export type PasskeyOptionsDto = {
  optionsJson: string
}

export type PasskeyAttestationRequestDto = {
  credentialJson: string
  name: string
}

export type PasskeyAssertionRequestDto = {
  credentialJson: string
  rememberMachine: boolean
  returnUrl: string
  userId: string
}

export type PasskeyDto = {
  id: string
  name: string
  createdAt: string
  transports: string[]
  isBackedUp: boolean
}

export type PasskeyCreateOptionsDto = {
  displayname: string
}
