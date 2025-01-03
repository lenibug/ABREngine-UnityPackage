/*blankPlate.js
 *
 * Construct a representation of a "blankPlate" for ABR. Instantiated into a data
 * impression When dragged into the composition panel.
 *
 * Copyright (C) 2021, University of Minnesota
 * Authors: Bridger Herman <herma582@umn.edu>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

import { COMPOSITION_LOADER_ID } from "./CompositionPanel.js";
import { Plate } from "./Plate.js";
import { uuid } from '../../../common/UUID.js';
import { globals } from '../../../common/globals.js';

export function BlankPlate(plateType) {
    let $blankPlate = $('<div>', {
        class: 'blankPlate rounded',
    }).data({
        plateType: plateType,
    });

    $blankPlate.append($('<div>', {
        class: 'plate-header rounded',
    }).append($('<p>', {
        class: 'plate-label',
        text: plateType,
    })));

    $blankPlate.append($('<img>', {
        class: 'blankPlate-thumbnail',
        src: `${STATIC_URL}compose/blankPlate_thumbnail/${plateType}.png`,
    }));


    $blankPlate.draggable({
        helper: 'clone',
        stop: (evt, ui) => {
            let $composition = $('#' + COMPOSITION_LOADER_ID);
            let pos = ui.helper.position();
            pos.left -= $composition.position().left;
            pos.top -= $composition.position().top;

            // Instantiate a new plate in the UI
            let imprId = uuid();
            let $instance = Plate(plateType, imprId, plateType, {
                position: pos,
            });

            $instance.appendTo($composition);

            let defaultName = globals.schema.definitions.Impression.properties.name.default;

            let impression = {
                plateType,
                uuid: imprId,
                name: defaultName ? defaultName : "Plate",
            };
            globals.stateManager.update('impressions/' + imprId, impression);
            globals.stateManager.update('uiData/compose/impressionData/' + imprId + '/position', pos);
        }
    });

    return $blankPlate;
}