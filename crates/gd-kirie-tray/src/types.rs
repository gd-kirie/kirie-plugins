use godot::prelude::*;

#[derive(Debug)]
pub struct Error {
    pub code: &'static str,
    pub message: String,
}

impl Error {
    pub fn new(code: &'static str, message: impl ToString) -> Self {
        Self {
            code,
            message: message.to_string(),
        }
    }

    pub fn unsupported(message: impl ToString) -> Self {
        Self::new("unsupported", message)
    }
}

impl From<tray_icon::Error> for Error {
    fn from(error: tray_icon::Error) -> Self {
        Self::new("native-error", error)
    }
}

impl From<tray_icon::menu::Error> for Error {
    fn from(error: tray_icon::menu::Error) -> Self {
        Self::new("native-error", error)
    }
}

impl From<godot::meta::error::ConvertError> for Error {
    fn from(error: godot::meta::error::ConvertError) -> Self {
        Self::new("invalid-argument", error)
    }
}

pub type Result<T> = std::result::Result<T, Error>;

pub fn required<T: FromGodot>(dictionary: &VarDictionary, key: &str) -> Result<T> {
    dictionary
        .get(key)
        .ok_or_else(|| Error::new("invalid-argument", format!("missing required field: {key}")))?
        .try_to::<T>()
        .map_err(Into::into)
}

pub fn optional<T: FromGodot>(dictionary: &VarDictionary, key: &str) -> Result<Option<T>> {
    dictionary
        .get(key)
        .map(|value| value.try_to::<T>().map_err(Into::into))
        .transpose()
}

pub fn nullable_string(value: &Variant) -> Result<Option<String>> {
    if value.is_nil() {
        return Ok(None);
    }
    Ok(Some(value.try_to::<String>()?))
}

pub fn optional_string(dictionary: &VarDictionary, key: &str) -> Result<Option<String>> {
    dictionary
        .get(key)
        .map(|value| nullable_string(&value))
        .unwrap_or(Ok(None))
}

#[derive(Debug)]
pub enum IconSource {
    Resource {
        path: String,
        scale: f64,
    },
    Rgba {
        rgba: Vec<u8>,
        width: u32,
        height: u32,
        scale: f64,
    },
    Native {
        name: String,
    },
    NativeName {
        name: String,
    },
    WindowsStock {
        id: i32,
    },
    WindowsPath {
        path: String,
        width: Option<u32>,
        height: Option<u32>,
    },
    WindowsResource {
        ordinal: u16,
        width: Option<u32>,
        height: Option<u32>,
    },
    WindowsResourceName {
        name: String,
        width: Option<u32>,
        height: Option<u32>,
    },
}

impl IconSource {
    pub fn read(value: &Variant) -> Result<Option<Self>> {
        if value.is_nil() {
            return Ok(None);
        }
        let dictionary = value.try_to::<VarDictionary>().map_err(Error::from)?;
        let kind = required::<String>(&dictionary, "kind")?;
        match kind.as_str() {
            "resource" => Ok(Some(Self::Resource {
                path: required::<String>(&dictionary, "path")?,
                scale: icon_scale(&dictionary)?,
            })),
            "rgba" => Ok(Some(Self::Rgba {
                rgba: required::<PackedByteArray>(&dictionary, "rgba")?.to_vec(),
                width: required(&dictionary, "width")?,
                height: required(&dictionary, "height")?,
                scale: icon_scale(&dictionary)?,
            })),
            "native" | "native-name" => {
                let name = required::<String>(&dictionary, "name")?;
                if kind == "native" {
                    Ok(Some(Self::Native { name }))
                } else {
                    Ok(Some(Self::NativeName { name }))
                }
            }
            "windows-stock" => Ok(Some(Self::WindowsStock {
                id: required(&dictionary, "id")?,
            })),
            "windows-path" => Ok(Some(Self::WindowsPath {
                path: required::<String>(&dictionary, "path")?,
                width: optional(&dictionary, "width")?,
                height: optional(&dictionary, "height")?,
            })),
            "windows-resource" => {
                let resource = required::<Variant>(&dictionary, "resource")?;
                let width = optional(&dictionary, "width")?;
                let height = optional(&dictionary, "height")?;
                if resource.get_type() == VariantType::STRING {
                    Ok(Some(Self::WindowsResourceName {
                        name: required::<String>(&dictionary, "resource")?,
                        width,
                        height,
                    }))
                } else {
                    Ok(Some(Self::WindowsResource {
                        ordinal: required(&dictionary, "resource")?,
                        width,
                        height,
                    }))
                }
            }
            kind => Err(Error::new(
                "invalid-argument",
                format!("unknown icon kind: {kind}"),
            )),
        }
    }
}

/// Reads the optional pixels-per-point scale of a custom icon. Godot callers may pass an integer.
fn icon_scale(dictionary: &VarDictionary) -> Result<f64> {
    let Some(value) = dictionary.get("scale") else {
        return Ok(1.0);
    };
    let scale = if value.get_type() == VariantType::INT {
        value.try_to::<i64>()? as f64
    } else {
        value.try_to::<f64>()?
    };
    // Upstream panics on scale factors that dpi rejects.
    if !tray_icon::dpi::validate_scale_factor(scale) {
        return Err(Error::new(
            "invalid-argument",
            format!("icon scale must be a normal positive number, got {scale}"),
        ));
    }
    Ok(scale)
}

#[derive(Debug)]
pub enum Accelerator {
    Physical(tray_icon::menu::accelerator::Accelerator),
    Logical(tray_icon::menu::accelerator::KeyAccelerator),
}

impl Accelerator {
    pub fn read(value: &Variant) -> Result<Option<Self>> {
        if value.is_nil() {
            return Ok(None);
        }
        let dictionary = value.try_to::<VarDictionary>().map_err(Error::from)?;
        let value = required::<String>(&dictionary, "value")?;
        match required::<String>(&dictionary, "kind")?.as_str() {
            "physical" => value
                .parse()
                .map(|value| Some(Self::Physical(value)))
                .map_err(|error| Error::new("invalid-argument", error)),
            "logical" => value
                .parse()
                .map(|value| Some(Self::Logical(value)))
                .map_err(|error| Error::new("invalid-argument", error)),
            kind => Err(Error::new(
                "invalid-argument",
                format!("unknown accelerator kind: {kind}"),
            )),
        }
    }
}

pub fn about_metadata(dictionary: &VarDictionary) -> Result<tray_icon::menu::AboutMetadata> {
    Ok(tray_icon::menu::AboutMetadata {
        name: optional_string(dictionary, "name")?,
        version: optional_string(dictionary, "version")?,
        short_version: optional_string(dictionary, "shortVersion")?,
        authors: dictionary
            .get("authors")
            .filter(|value| !value.is_nil())
            .map(|value| value.try_to::<Vec<GString>>())
            .transpose()?
            .map(|values| values.into_iter().map(|value| value.to_string()).collect()),
        comments: optional_string(dictionary, "comments")?,
        copyright: optional_string(dictionary, "copyright")?,
        license: optional_string(dictionary, "license")?,
        website: optional_string(dictionary, "website")?,
        website_label: optional_string(dictionary, "websiteLabel")?,
        credits: optional_string(dictionary, "credits")?,
        icon: IconSource::read(&dictionary.get_or_nil("icon"))?
            .map(crate::icons::menu)
            .transpose()?,
    })
}

pub fn guid(value: &str) -> Result<u128> {
    let valid = value.len() == 32
        || (value.len() == 36
            && [8, 13, 18, 23]
                .into_iter()
                .all(|i| value.as_bytes()[i] == b'-'));
    if !valid {
        return Err(Error::new(
            "invalid-argument",
            "GUID must be 32 hexadecimal digits or a canonical UUID",
        ));
    }
    let digits: String = value.chars().filter(|c| *c != '-').collect();
    if digits.len() != 32 || !digits.bytes().all(|c| c.is_ascii_hexdigit()) {
        return Err(Error::new("invalid-argument", "invalid GUID"));
    }
    u128::from_str_radix(&digits, 16).map_err(|error| Error::new("invalid-argument", error))
}

pub fn reply<T: ToGodot>(operation: impl FnOnce() -> Result<T>) -> VarDictionary {
    match operation() {
        Ok(value) => vdict! { "ok" => true, "value" => &value.to_variant() },
        Err(error) => vdict! {
            "ok" => false,
            "error" => &vdict! { "code" => error.code, "message" => error.message.as_str() },
        },
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn rejects_malformed_guids() {
        assert!(guid("aa-aa-aa-aa-aa-aa-aa-aa-aa-aa-aa-aa-aa-aa-aa-aa").is_err());
        assert_eq!(
            guid("12345678-1234-5678-9abc-def012345678").unwrap(),
            0x12345678123456789abcdef012345678
        );
    }
}
